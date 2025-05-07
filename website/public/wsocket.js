// compatible with ngrok despite http requirements
const socket = new WebSocket('ws://localhost:5251/connect')
const feed = document.getElementById('cameraFeed');


socket.onopen = event => {
    message = JSON.stringify({  authentication: "", cameraGuid: "0557697d-6523-4792-8d69-3a7f65109624", fps: 10 })
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
