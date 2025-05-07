import requests as rq
import websockets
import asyncio

connect_endpoint = "connectcamera"

async def connect_camera_websocket(address, host_port, camera_guid):
    if host_port is not None:
        url = f"{address}:{host_port}/{connect_endpoint}"
    else:
        url = f"{address}/{connect_endpoint}"

    print("Attempting connection to WebSocket...")
    # send extra_headers={ 'cameraGuid' : camera_guid }, cannot have extra headers though
    # come up with solution...

    websocket = await websockets.connect(url)
    await asyncio.sleep(5)
    await websocket.send(camera_guid)

    return websocket