import requests
import json

base_url = "http://localhost:5291/"

def postrequest(url, header, body=""):
    print(f"URL: {base_url + url}\nHeaders: {header}")
    return requests.post(base_url + url, headers=header, json=body)

def register_camera():
    pass

def save_snapshot():
    pass

def testget():
    return requests.get(base_url)
