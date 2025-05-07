using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraSession
{
	public CameraSession(Camera camera, WebSocket socket)
	{
		Camera = camera;
		Socket = socket;
	}
	
	public Camera Camera { get; init; }
	public WebSocket Socket { get; init; }
	public CancellationTokenSource CancellationTokenSource { get; init; }
	public byte[]? CurrentSnapshot { get; set; }
	public DateTime LastSnapshotReceived { get; set; }
	
	public Action<CameraSession>? OnSnapshotReceived { get; set; }

	public List<CameraViewerSession> Subscribed { get; } = new();

	void SendRequestToSend()
	{
		
	}

	public static async Task<string?> ReceiveCameraGuidFromServer(WebSocket webSocket)
	{
		try
		{
			CancellationToken token = new CancellationTokenSource().Token;
			ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[256]);
			var response = await webSocket.ReceiveAsync(buffer, token);
			if (!response.EndOfMessage)
			{
				Console.WriteLine("Received camera GUID was too long, aborting");
				await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, string.Empty, token);
				return null;
			}
			return System.Text.Encoding.Default.GetString(buffer.Slice(0, response.Count));
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return null;
		}
	}
	
	private async Task BroadcastFrameToAllSubscribers(CancellationToken token)
	{
		foreach (var viewer in Subscribed)
		{
			ArraySegment<byte> message = new ArraySegment<byte>(CurrentSnapshot);
			await viewer.Socket.SendAsync(message, WebSocketMessageType.Binary, true, token);
		}
	}

	public async Task HandleAsync(CancellationToken token)
	{
		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[65536]);
		if (buffer.Array == null) return;
		
		MemoryStream stream = new MemoryStream();

		try
		{
			while (!token.IsCancellationRequested)
			{
				Console.WriteLine($"Waiting for data, socket state is {Socket.State}");
				var response = await Socket.ReceiveAsync(buffer, token);

				if (response.CloseStatus.HasValue)
				{
					Console.WriteLine("Connection closed!");
				}

				stream.Write(buffer.Array, buffer.Offset, response.Count);

				if (response.EndOfMessage)
				{
					CurrentSnapshot = stream.ToArray();
					LastSnapshotReceived = DateTime.Now;
					OnSnapshotReceived?.Invoke(this);

					Console.WriteLine("Image received!");

					await BroadcastFrameToAllSubscribers(token);

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
	
}