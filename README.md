# Group 63 Security Camera Project

## Sections

1. Camera code
2. Server backend
3. Webpage frontend

## Manual

### Camera Setup (Raspberry Pi)

#### 1. Installation

##### Hardware Requirements
- Raspberry Pi 4 or 5
- USB or CSI camera (compatible with OpenCV)
- Reliable power supply
- Internet connection (Ethernet or Wi-Fi)
- *(Optional)* Monitor and keyboard for debugging

##### Software Requirements
- Raspberry Pi OS (Lite or Desktop)
- Python 3.7+
- Install required packages:

```bash
sudo apt update
sudo apt install python3-pip python3-opencv libopencv-dev
pip3 install flask websockets requests
```

> **Note:** OpenCV is used for image capture and detection. Flask is used to expose a local API.

---

#### 2. Starting the script

1. Navigate to the webcam folder:
    ```bash
    cd Group63-SecurityCamera/webcam
    ```

2. Run the script:
    ```bash
    python3 main.py
    ```

3. *(Optional)* Run in debug mode (disables video streaming):
    ```bash
    python3 main.py --debug
    ```

---

#### 3. Command Input

The system supports one command-line argument:

- `--debug` : disables streaming (local preview only)

Example:
```bash
python3 main.py --debug
```

Use the **`q`** key to exit preview mode.

---

#### 4. Exporting Results

##### Live Human Detection
- Bounding boxes appear on moving humans.
- Timestamps are printed in the terminal when detected.

##### REST API (via Flask)
- `GET /status` – Detection status:
    ```json
    {"human_detected": true}
    ```
- `GET /snapshot` – Latest detected frame (JPEG)

Example call:
```bash
curl http://<PI_IP>:5001/status
```

---

#### 5. Shutdown
- Press **`q`** to quit the camera feed
- Or terminate with `Ctrl+C` in the terminal

To shut down the Raspberry Pi:
```bash
sudo shutdown now
```



### Server Setup

### Website Setup

#### 1. Navigate to the 'website' folder

```bash
cd website
```

---

#### 2. Install dependencies

```bash
npm install
npm install http-proxy-middleware
```

---

#### 3. Start the frontend server

```bash
npm start
```

---

#### 4. Open your browser and go to

```bash
http://localhost:3000
```
