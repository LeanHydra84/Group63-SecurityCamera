import requests
import json

base_url = "http://localhost:5291/"

def postrequest(url, header, body=""):
    print(f"URL: {base_url + url}\nHeaders: {header}")
    return requests.post(base_url + url, headers=header, json=body)

def create_user(email, password):
    return postrequest("/newuser", { "Email" : "guslindell2@gmail.com", "Password" : "mypassword" }, "Gus")

def login(email, password):
    return postrequest("/login", { "Email" : "guslindell2@gmail.com", "Password" : "mypassword" }, "")

def register_camera(name, auth):
    return postrequest("/registercamera", { "Email" : "guslindell2@gmail.com", "Authentication" : auth }, json.dumps({ "Name" : name }))
    
def save_snapshot():
    pass

def testget():
    return requests.get(base_url)
