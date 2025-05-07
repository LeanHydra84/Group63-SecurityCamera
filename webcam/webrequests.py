import requests as rq
import websockets
import asyncio

connect_endpoint = "connect_camera"

async def connect_camera_websocket(address, host_port, camera_guid):
    if host_port is not None:
        url = f"{address}:{host_port}/{connect_endpoint}"
    else:
        url = f"{address}/{connect_endpoint}"

    print("Connecting to websocket...")

    websocket = await websockets.connect(url)
    await websocket.send(camera_guid)

    print("Websocket connected")

    return websocket