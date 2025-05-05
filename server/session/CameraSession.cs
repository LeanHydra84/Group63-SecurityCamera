using System.Net.WebSockets;

namespace SecurityCameraServer;

public class CameraSession
{
	private static HttpClient _client = new HttpClient();
	public Camera Camera { get; set; }
	public WebSocket? Socket { get; set; }
	public CancellationTokenSource? CancellationTokenSource { get; set; }
	public byte[]? CurrentSnapshot { get; set; }
	public DateTime LastSnapshotReceived { get; set; }
	
	public Action<CameraSession>? OnSnapshotReceived;

	// public async Task<byte[]?> RequestSnapshotAsync()
	// {
	// 	var message = new HttpRequestMessage(HttpMethod.Get, $"{CameraURI}/snapshot");
	// 	var response = await _client.SendAsync(message);
	// 	
	// 	return await response.Content.ReadAsByteArrayAsync();
	// }

}