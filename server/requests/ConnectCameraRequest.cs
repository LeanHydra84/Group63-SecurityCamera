using Newtonsoft.Json;

namespace SecurityCameraServer;

[Serializable]
public class ConnectCameraRequest
{
	[JsonProperty("cameraGuid")]
	public string? CameraGuid { get; set; }
	
	[JsonProperty("sourceIp")]
	public string? SourceIp { get; set; }
	
	[JsonProperty("sourcePort")]
	public int SourcePort { get; set; }
}