using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraSession : SocketHandlerBase
{
	public CameraSession(Camera camera, WebSocket socket) : base(socket)
	{
		Camera = camera;
	}
	
	public Camera Camera { get; init; }
	public byte[]? CurrentSnapshot { get; private set; }
	public DateTime LastSnapshotReceived { get; private set; }

	public event EventHandler? OnSnapshotReceived;

	public List<CameraViewerSession> Subscribed { get; } = new();


	private void MarkFrameForBroadcast(CancellationToken token)
	{
		foreach (var viewer in Subscribed)
		{
			viewer.MarkFrameReady();
		}
	}

	public override async Task HandleAsync()
	{
		CancellationToken token = CancellationTokenSource.Token;	
		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[65536]);
		if (buffer.Array == null) return;
		
		MemoryStream stream = new MemoryStream();

		try
		{
			while (!token.IsCancellationRequested)
			{
				var response = await Socket.ReceiveAsync(buffer, token);
				if (response.CloseStatus.HasValue)
				{
					Console.WriteLine("Connection closed!");
					return;
				}

				stream.Write(buffer.Array, buffer.Offset, response.Count);

				if (response.EndOfMessage)
				{
					CurrentSnapshot = stream.ToArray();
					LastSnapshotReceived = DateTime.Now;
					OnSnapshotReceived?.Invoke(this, EventArgs.Empty);

					// Console.WriteLine("Image received!");

					// I don't think this should be awaited? Don't want this to be blocking, but sockets have issues in un-awaited functions sometimes
					MarkFrameForBroadcast(token);

					stream.SetLength(0);
				}
			}
		}
		catch (Exception e)
		{
			Console.Write("Socket closed; ");
			Console.WriteLine(e.Message);
			Application.ActiveCameras.CullSession(Camera);
		}
	}

	public override void EndSession()
	{
		foreach (var user in Subscribed)
		{
			user.EndSession();
		}

		base.EndSession();
	}
	
}