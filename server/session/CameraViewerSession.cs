using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraViewerSession : SocketHandlerBase
{
    public LoginSession? Session { get; }
    public CameraSession Camera { get; }

    public CameraViewerSession(WebSocket socket, LoginSession? session, CameraSession camera) : base(socket)
    {
        Session = session;
        Camera = camera;
    }
    
}