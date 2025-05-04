

namespace SecurityCameraServer
{
	public class Camera
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public string CameraGUID { get; set; }
		public User Owner { get; set; }
	}
}