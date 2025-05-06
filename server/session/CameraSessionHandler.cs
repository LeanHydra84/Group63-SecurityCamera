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
		session.CancellationTokenSource?.CancelAsync();
		activeSessions.Remove(camera.CameraGuid);
		_ = session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
	}

	public bool RegisterSession(Camera camera, WebSocket socket, Action<CameraSession>? onSessionCreated = null)
	{
		if (activeSessions.ContainsKey(camera.CameraGuid))
			CullSession(camera);

		CameraSession session = new(camera, socket)
		{
			OnSnapshotReceived = onSessionCreated,
			CancellationTokenSource = new CancellationTokenSource(),
		};

		activeSessions[camera.CameraGuid] = session;

		_ = HandleAsync(session, session.CancellationTokenSource.Token);
		
		return true;
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

	private static async Task BroadcastFrameToAllSubscribers(CameraSession session, CancellationToken token)
	{
		foreach (var viewer in session.Subscribed)
		{
			ArraySegment<byte> message = new ArraySegment<byte>(session.CurrentSnapshot);
			await viewer.Socket.SendAsync(message, WebSocketMessageType.Binary, true, token);
		}
	}

	private async Task HandleAsync(CameraSession session, CancellationToken token)
	{
		WebSocket socket = session.Socket;

		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[65536]);
		if (buffer.Array == null) return;
		
		MemoryStream stream = new MemoryStream();

		try
		{
			while (!token.IsCancellationRequested)
			{
				Console.WriteLine($"Waiting for data, socket state is {socket.State}");
				var response = await socket.ReceiveAsync(buffer, token);

				if (response.CloseStatus.HasValue)
				{
					Console.WriteLine("Connection closed!");
				}

				stream.Write(buffer.Array, buffer.Offset, response.Count);

				if (response.EndOfMessage)
				{
					session.CurrentSnapshot = stream.ToArray();
					session.LastSnapshotReceived = DateTime.Now;
					session.OnSnapshotReceived?.Invoke(session);

					Console.WriteLine("Image received!");

					await BroadcastFrameToAllSubscribers(session, token);

					stream.SetLength(0);
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			CullSession(session.Camera);
		}
	}
	
}