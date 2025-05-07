import cv2
import time
import datetime
import argparse
import socket
from flask import Flask, jsonify, Response
import threading
from enum import Enum
import webrequests as wr
import asyncio

class StreamingMode(Enum):
    Disabled = 0
    GStream = 1
    WebSocket = 2

# setup flask
# app = Flask(__name__)
detection_status = {"human_detected": False}
latest_frame = None
last_detected_time = 0  # Global tracking
STATUS_HOLD_SECONDS = 3

# @app.route('/status')
# def status():
#     print(f"[DEBUG] /status requested. Returning: {detection_status}")
#     return jsonify(detection_status)

# @app.route('/snapshot')
# def snapshot():
#     global latest_frame
#     if latest_frame is not None:
#         _, jpeg = cv2.imencode('.jpg', latest_frame)
#         return Response(jpeg.tobytes(), mimetype='image/jpeg')
#     return "No frame available", 404

# def run_flask():
#     import logging
#     log = logging.getLogger('werkzeug')
#     log.setLevel(logging.ERROR)  # suppress request logs
#     app.run(host='0.0.0.0', port=5001)

# flask_thread = threading.Thread(target=run_flask, daemon=True)
# flask_thread.start()

# ---------- cmd setup ----------
parser = argparse.ArgumentParser(description="Security camera stream with human detection.")
parser.add_argument('--debug', action='store_true', help="Disable GStreamer streaming (debug mode)")
args = parser.parse_args()
#ENABLE_STREAMING = False #not args.debug
STREAMING_MODE = StreamingMode.WebSocket
# ---------------------------------------

# configs
HOST_IP = "ws://localhost"       # placeholder
HOST_PORT = 5251            # placeholder
CAMERA_GUID = '0557697d-6523-4792-8d69-3a7f65109624'            # placeholder
USE_LOCAL_PREVIEW = True    # false if headless (no local display)
FRAME_SIZE = (640, 480)
FPS = 10
COOLDOWN_SECONDS = 5
BOX_DISPLAY_SECONDS = 2

# Human detector
hog = cv2.HOGDescriptor()
hog.setSVMDetector(cv2.HOGDescriptor_getDefaultPeopleDetector())

# local ip
def get_local_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"

LOCAL_IP = get_local_ip()

print(f"[INFO] Local device IP: {LOCAL_IP}")
if STREAMING_MODE is StreamingMode.GStream:
    print(f"[INFO] (GStream) Streaming to {HOST_IP}:5000")
elif STREAMING_MODE is StreamingMode.WebSocket:
    print(f"[INFO] (WebSocket) Streaming to {HOST_IP}:{HOST_PORT}")
else:
    print("[INFO] Debug mode: streaming is disabled")

# gstreamer setup writer (if enabled)
out = None
wsocket = None
if STREAMING_MODE is StreamingMode.GStream:
    gst_str = (
        f'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=ultrafast ! '
        f'rtph264pay config-interval=1 pt=96 ! udpsink host={HOST_IP} port=5000'
    )
    out = cv2.VideoWriter(gst_str, cv2.CAP_GSTREAMER, 0, FPS, FRAME_SIZE, True)
    if not out.isOpened():
        print("[ERROR] Failed to open GStreamer pipeline.")
        exit(1)
elif STREAMING_MODE is StreamingMode.WebSocket:
    # wsocket = asyncio.run(wr.connect_camera_websocket(HOST_IP, HOST_PORT, CAMERA_GUID))
    pass
else:
    print("[INFO] Running in DEBUG MODE — GStreamer streaming disabled.")

# open cam
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("[ERROR]: Could not open webcam.")
    exit(1)

print(f"[INFO] {'Streaming' if STREAMING_MODE is not StreamingMode.Disabled else 'Preview-only'} mode started. Press 'q' to quit.")

print("------------- Configuration -------------")
print(f"[INFO] Local IP       : {LOCAL_IP}")
print(f"[INFO] Host IP        : {HOST_IP}")
print(f"[INFO] Streaming      : {'ENABLED' if STREAMING_MODE is not StreamingMode.Disabled else 'DISABLED (Debug Mode)'}")
print(f"[INFO] Preview        : {'ENABLED' if USE_LOCAL_PREVIEW else 'DISABLED'}")
print("------------------------------------------")

async def send_loop():
    global latest_frame
    global last_detected_time # Global tracking

    # initiate tracking
    prev_gray = None
    last_detection_time = 0
    last_box_time = 0
    recent_boxes = []

    wsocket = await wr.connect_camera_websocket(HOST_IP, HOST_PORT, CAMERA_GUID)

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frame = cv2.resize(frame, FRAME_SIZE)
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

        # motion detect
        motion_detected = False
        if prev_gray is not None:
            diff = cv2.absdiff(prev_gray, gray)
            _, thresh = cv2.threshold(diff, 25, 255, cv2.THRESH_BINARY)
            motion_area = cv2.countNonZero(thresh)
            motion_detected = motion_area > 5000

        prev_gray = gray.copy()
        new_boxes = []
        current_time = time.time()

        if motion_detected:
            boxes, _ = hog.detectMultiScale(frame, winStride=(8, 8))
            new_boxes = boxes
            if len(boxes) > 0:
                last_detected_time = current_time
                if current_time - last_detection_time > COOLDOWN_SECONDS:
                    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                    print(f"[{timestamp}] Moving human detected.")
                    last_detection_time = current_time
                    last_box_time = current_time
                    recent_boxes = boxes
                latest_frame = frame.copy()

        # hide boxes (if timeout)
        if current_time - last_box_time < BOX_DISPLAY_SECONDS:
            for (x, y, w, h) in recent_boxes:
                cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
        else:
            recent_boxes = []

        # stream frame (if enabled)
        if STREAMING_MODE is StreamingMode.GStream and out is not None:
            out.write(frame)
        elif STREAMING_MODE is StreamingMode.WebSocket and wsocket is not None:
            _, jpeg = cv2.imencode('.jpg', frame)
            await wsocket.send(jpeg.tobytes())
            print("Sending frame")

        # optional local preview
        if USE_LOCAL_PREVIEW:
            cv2.imshow("Preview", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

        # update shared state
        latest_frame = frame.copy()
        detection_status["human_detected"] = (current_time - last_detected_time) < STATUS_HOLD_SECONDS
        
        await asyncio.sleep(1 / FPS)

asyncio.run(send_loop())

# Cleanup
cap.release()
if out:
    out.release()
if wsocket:
    wsocket.close()
cv2.destroyAllWindows()