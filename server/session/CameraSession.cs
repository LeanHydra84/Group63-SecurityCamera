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
	public CancellationTokenSource? CancellationTokenSource { get; init; }
	public byte[]? CurrentSnapshot { get; set; }
	public DateTime LastSnapshotReceived { get; set; }
	
	public Action<CameraSession>? OnSnapshotReceived { get; init; }

	public List<CameraViewerSession> Subscribed { get; } = new();

}