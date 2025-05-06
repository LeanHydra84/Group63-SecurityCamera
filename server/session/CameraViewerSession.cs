using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraViewerSession
{
    public WebSocket Socket { get; }
    public LoginSession Session { get; }
    public CameraSession Camera { get; }

    public CameraViewerSession(WebSocket socket, LoginSession session, CameraSession camera)
    {
        Socket = socket;
        Session = session;
        Camera = camera;
    }
    
}