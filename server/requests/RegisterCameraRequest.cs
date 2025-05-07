#nullable disable

namespace SecurityCameraServer;

public class RegisterCameraRequest
{
	public string Name { get; set; }
	public string RequestedGUID { get; set; }
	
	// other data like framerate??
}