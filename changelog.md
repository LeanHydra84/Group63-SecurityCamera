## Change Log (January 21st – May 5th)

Running document through the beginning of the project listing all work achieved by our group.

### Hardware / System Integration
- Designed and built the top-down hardware layout for the Raspberry Pi-based camera module.
- Connected and configured USB/CSI camera with Raspberry Pi hardware.
- Verified network-level communication between Raspberry Pi and host system using local IP addressing.

### Software – Raspberry Pi (`webcam` folder)
- Implemented real-time camera stream capture using OpenCV.
- Integrated HOG-based human detection logic with bounding box overlay.
- Created RESTful Flask API endpoints (`/status`, `/snapshot`) for communication with the host PC.
- Enabled motion detection using frame differencing and thresholding techniques.
- Built multithreaded architecture to run Flask API server alongside image processing loop.
- Added support for GStreamer and WebSocket streaming modes.
- Developed `webrequests.py` module to manage WebSocket connections and data transfer.

### Software – Host PC / Backend
- Developed server backend to receive camera stream and manage detection logs.
- Implemented WebSocket and GStreamer-based video stream reception.
- Built system for timestamping and recording motion or human detection events.

### Frontend – Website Interface
- Created web interface to view detection status and request snapshots from the Raspberry Pi.
- Enabled live polling of `/status` endpoint for real-time detection updates.
- Designed a basic UI to allow switching views and checking image feeds.

### Documentation and Design
- Completed weekly progress reports (Weeks 1 through 11) outlining goals, deliverables, and blockers.
- Authored a full design document including:
  - Network architecture diagram
  - UML class diagram of software system
  - Top-down hardware layout
- Wrote user manuals for setting up and operating the Raspberry Pi unit.
- Maintained and finalized the `README.md` file with full setup and usage instructions.
