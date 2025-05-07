using Newtonsoft.Json;

namespace SecurityCameraServer;

public class ViewStreamRequest
{
	[JsonProperty("authentication")]
	public string? Authentication { get; set; }
	
	[JsonProperty("cameraGUID")]
	public string? CameraGuid { get; set; }
	
	[JsonProperty("fps")]
	public int RequestedFps { get; set; }
}