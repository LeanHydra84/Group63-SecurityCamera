namespace SecurityCameraServer;

public class CameraSessionHandler
{
	
	private Dictionary<string, CameraSession> activeSessions = new();

	public void CullSession(Camera camera)
	{
		// perform stream closing logic

		activeSessions.Remove(camera.CameraGUID);
	}

	public bool RegisterSession(Camera camera, string sourceUrl, int sourcePort)
	{
		if (activeSessions.ContainsKey(camera.CameraGUID))
			CullSession(camera);
		
		CameraSession session = new CameraSession
		{
			Camera = camera,
			BaseAddress = sourceUrl,
			CameraPort = sourcePort,
		};
		
		activeSessions[camera.CameraGUID] = session;
		return true;
	}

	public CameraSession? GetSession(Camera camera)
	{
		activeSessions.TryGetValue(camera.CameraGUID, out var session);
		return session;
	}

	public CameraSession? GetSession(string cameraGuid)
	{
		activeSessions.TryGetValue(cameraGuid, out var session);
		return session;
	}
	
}