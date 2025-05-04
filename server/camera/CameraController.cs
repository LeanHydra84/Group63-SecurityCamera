namespace SecurityCameraServer;

public class CameraController
{

	public Camera? RegisterNewCamera(User owner, RegisterCameraData data)
	{
		
		string guid = Guid.NewGuid().ToString();
		if (data.RequestedGUID != null)
		{
			bool guidIsTaken = Application.Database.Context.Cameras.Any(a => a.CameraGUID == data.RequestedGUID);
			if (!guidIsTaken)
			{
				guid = data.RequestedGUID;
			}
		}
		
		Camera newCamera = new Camera
		{
			CameraGUID = guid,
			Owner = owner,
			Name = data.Name,
		};

		Application.Database.AddCamera(newCamera);
		
		return newCamera;
	}
	
}