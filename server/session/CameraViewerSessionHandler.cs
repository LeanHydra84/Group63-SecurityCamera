using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraViewerSessionHandler
{
    public const int DefaultFps = 15;
    
    private readonly Dictionary<int, CameraViewerSession> activeSessions = new();

    private List<CameraViewerSession> cameraViewerSessions = new();
    
    public void CullSession(CameraViewerSession session)
    {
        session.Camera.Subscribed.Remove(session);
        _ = session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        int id = session.Session.User.ID;
        activeSessions.Remove(id);
    }
    
    public CameraViewerSession RegisterSession(LoginSession? session, CameraSession camera, WebSocket socket, int requestedFps)
    {
        if (requestedFps <= 0)
            requestedFps = DefaultFps;
        
        CameraViewerSession cvs = new CameraViewerSession(socket, session, camera, requestedFps);
        // activeSessions[session.User.ID] = cvs;
        cameraViewerSessions.Add(cvs);

        Application.ActiveCameras.SubscribeViewer(camera.Camera.CameraGuid, cvs);
        return cvs;
    }
    
    public void DisposeAll()
    {
        foreach (var session in cameraViewerSessions)
        {
            session.EndSession();
        }
		
        activeSessions.Clear();
        cameraViewerSessions.Clear();
    }
    
}