// compatible with ngrok despite http requirements
const socket = new WebSocket('ws://localhost:5251/connect')
const feed = document.getElementById('cameraFeed');

const urlParams = new URLSearchParams(window.location.search);
const guid = urlParams.get('cam')

console.log(guid)

function parseCookie(cname)
{
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for(let i = 0; i <ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) == ' ') {
        c = c.substring(1);
      }
      if (c.indexOf(name) == 0) {
        return c.substring(name.length, c.length);
      }
    }
    return "";
}

function createAuthHeaders()
{
    let username = parseCookie("username")
    let auth = parseCookie("authentication")

    return {
        "username" : username,
        "authentication" : auth,
        "Content-type": "application/json; charset=UTF-8"
    }
}

socket.onopen = event => {
    let auth = createAuthHeaders().authentication
    message = JSON.stringify({ authentication: auth, cameraGuid: guid, fps: 10 })
    socket.send(message)
}

socket.onclose = event => {
    console.log('Socket closed')
}

function setImage(data) {
    let blob = new Blob([data], { type: "image/jpeg" });
    var imageUrl = URL.createObjectURL(blob);
    feed.src = imageUrl;
}

socket.onmessage = function(event) {
    console.log('receiving message...')
    setImage(event.data);
    return false;
}