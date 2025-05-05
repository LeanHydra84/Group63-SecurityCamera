import requests as rq
import websockets
import asyncio

connect_endpoint = "connectcamera"

async def connect_camera_websocket(address, host_port, camera_guid):
    if host_port is not None:
        url = f"{address}:{host_port}/{connect_endpoint}"
    else:
        url = f"{address}/{connect_endpoint}"

    websocket = await websockets.connect(url, extra_headers={ 'cameraGuid' : camera_guid })
    websocket.send

    return websockets