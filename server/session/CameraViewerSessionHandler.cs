using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraViewerSessionHandler
{
    private readonly Dictionary<int, CameraViewerSession> activeSessions = new();

    public void CullSession(CameraViewerSession session)
    {
        session.Camera.Subscribed.Remove(session);
        _ = session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        int id = session.Session.User.ID;
        activeSessions.Remove(id);
    }
    
    public void RegisterSession(LoginSession session, CameraSession camera, WebSocket socket)
    {
        CameraViewerSession cvs = new CameraViewerSession(socket, session, camera);
        activeSessions[session.User.ID] = cvs;

        Application.ActiveCameras.SubscribeViewer(camera.Camera.CameraGuid, cvs);
    }
    
}