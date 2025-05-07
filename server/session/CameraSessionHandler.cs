using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraSessionHandler
{
	
	private readonly Dictionary<string, CameraSession> activeSessions = new();

	public void CullSession(Camera camera)
	{
		if (!activeSessions.ContainsKey(camera.CameraGuid))
			return;
		CameraSession session = activeSessions[camera.CameraGuid];
		activeSessions.Remove(camera.CameraGuid);
		_ = session.CancellationTokenSource?.CancelAsync();
		_ = session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
	}

	public CameraSession RegisterSession(Camera camera, WebSocket socket, EventHandler? onSessionCreated = null)
	{
		if (activeSessions.ContainsKey(camera.CameraGuid))
			CullSession(camera);

		CameraSession session = new(camera, socket);
		session.OnSnapshotReceived += onSessionCreated;
		
		activeSessions[camera.CameraGuid] = session;
		
		return session;
	}

	public bool SubscribeViewer(string cameraGuid, CameraViewerSession viewer)
	{
		if (!activeSessions.ContainsKey(cameraGuid))
			return false;
		
		CameraSession camera = activeSessions[cameraGuid];
		camera.Subscribed.Add(viewer);

		return true;
	}

	public CameraSession? GetSession(Camera camera)
	{
		activeSessions.TryGetValue(camera.CameraGuid, out var session);
		return session;
	}

	public CameraSession? GetSession(string cameraGuid)
	{
		activeSessions.TryGetValue(cameraGuid, out var session);
		return session;
	}

	public void DisposeAll()
	{
		foreach (var kvp in activeSessions)
		{
			kvp.Value.EndSession();
		}
		
		activeSessions.Clear();
	}

}