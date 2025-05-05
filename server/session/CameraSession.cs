namespace SecurityCameraServer;

public class CameraSession
{
	private static HttpClient _client = new HttpClient();
	
	public Camera Camera { get; set; }
	public string BaseAddress { get; set; }
	public int CameraPort { get; set; }

	public string CameraURI
	{
		get
		{
			if (CameraPort == 0) return BaseAddress;
			return $"{BaseAddress}:{CameraPort}";
		}
	}

	public async Task<byte[]?> RequestSnapshotAsync()
	{
		var message = new HttpRequestMessage(HttpMethod.Get, $"{CameraURI}/snapshot");
		var response = await _client.SendAsync(message);
		
		return await response.Content.ReadAsByteArrayAsync();
	}

}