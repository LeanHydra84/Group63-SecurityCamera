#nullable disable

using Microsoft.EntityFrameworkCore;

namespace SecurityCameraServer
{
	public class Camera
	{
		public int ID { get; set; }
		public string Name { get; init; }
		public string CameraGuid { get; init; }
		public User Owner { get; init; }
		public bool IsPublic { get; init; }
	}
}