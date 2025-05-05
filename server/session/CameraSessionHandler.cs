using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraSessionHandler
{
	
	private Dictionary<string, CameraSession> activeSessions = new();

	public void CullSession(Camera camera)
	{
		// perform stream closing logic

		activeSessions.Remove(camera.CameraGUID);
	}

	public bool RegisterSession(Camera camera, WebSocket socket, Action<CameraSession>? onSessionCreated = null)
	{
		if (activeSessions.ContainsKey(camera.CameraGUID))
			CullSession(camera);

		CameraSession session = new()
		{
			Camera = camera,
			Socket = socket,
			OnSnapshotReceived = onSessionCreated
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

	private async Task HandleAsync(CameraSession session, CancellationToken token)
	{
		WebSocket? socket = session.Socket;
		if (socket == null) return;

		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
		MemoryStream stream = new MemoryStream();
		
		try
		{
			while (!token.IsCancellationRequested)
			{
				var response = await socket.ReceiveAsync(session.CurrentSnapshot, token);
				stream.Write(buffer.Array, buffer.Offset, response.Count);

				if (response.EndOfMessage)
				{
					session.CurrentSnapshot = stream.ToArray();
					session.LastSnapshotReceived = DateTime.Now;
					session.OnSnapshotReceived?.Invoke(session);
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
	
}