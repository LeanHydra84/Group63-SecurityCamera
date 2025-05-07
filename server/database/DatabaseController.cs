using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;

namespace SecurityCameraServer
{
    public sealed class DatabaseController
    {

        internal SecurityCameraDBContext Context => dbContext;

        private SecurityCameraDBContext dbContext;

        public DatabaseController()
        {
            dbContext = new SecurityCameraDBContext();
        }

        // USER

        public User? GetLightUserFromUsername(string username)
        {
            return dbContext.Users.SingleOrDefault(a => a.Username == username);
        }

        public User? GetHeavyUserFromUsername(string username)
        {
            return dbContext.Users.Include(a => a.Cameras).SingleOrDefault(a => a.Username == username);
        }

        public User? GetHeavyUserFromId(int id)
        {
            return dbContext.Users.Include(a => a.Cameras).SingleOrDefault(a => a.ID == id);
        }

        public bool AddUser(User user)
        {
            if (user.Username == null) return false;
            if (GetLightUserFromUsername(user.Username) != null)
                return false;
            dbContext.Users.Add(user);
            dbContext.SaveChanges();
            return true;
        }

        public void RemoveUserByUsername(string username)
        {
            User? user = GetLightUserFromUsername(username);
            if (user == null) return;
            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
        }

        public IEnumerable<User> GetAllUsers()
        {
            return dbContext.Users;
        }

        public void DeleteAllUsers(bool AreYouSure)
        {
            if (!AreYouSure) return;
            dbContext.Users.RemoveRange(GetAllUsers());
            dbContext.SaveChanges();
        }

        // CAMERA

        public Camera? RegisterNewCamera(User owner, RegisterCameraRequest request)
        {
		
            string guid = Guid.NewGuid().ToString();
            if (request.RequestedGUID != null)
            {
                bool guidIsTaken = Application.Database.Context.Cameras.Any(a => a.CameraGuid == request.RequestedGUID);
                if (!guidIsTaken)
                {
                    guid = request.RequestedGUID;
                }
            }
		
            Camera newCamera = new Camera
            {
                CameraGuid = guid,
                Owner = owner,
                Name = request.Name,
            };

            AddCamera(newCamera);
		
            return newCamera;
        }
        
        public void AddCamera(Camera camera)
        {
            dbContext.Cameras.Add(camera);
            dbContext.SaveChanges();
        }

        public List<Camera>? GetLightAllCameras(string username)
        { 
            User? user = GetHeavyUserFromUsername(username);
            return user?.Cameras?.ToList();
        }

        public Camera? GetCamera(string guid)
        {
            return dbContext.Cameras.Include(a => a.Owner).FirstOrDefault(a => a.CameraGuid == guid);
        }

    }
}