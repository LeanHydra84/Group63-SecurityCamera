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

        public User? GetLightUserFromEmail(string email)
        {
            return dbContext.Users.SingleOrDefault(a => a.EMail == email);
        }

        public User? GetHeavyUserFromEmail(string email)
        {
            return dbContext.Users.Include(a => a.Cameras).SingleOrDefault(a => a.EMail == email);
        }

        public User? GetHeavyUserFromID(int id)
        {
            return dbContext.Users.Include(a => a.Cameras).SingleOrDefault(a => a.ID == id);
        }

        public bool AddUser(User user)
        {
            if (user.EMail == null) return false;
            if (GetLightUserFromEmail(user.EMail) != null)
                return false;
            dbContext.Users.Add(user);
            dbContext.SaveChanges();
            return true;
        }

        public void RemoveUserByEmail(string email)
        {
            User? user = GetLightUserFromEmail(email);
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

        public void AddCamera(Camera camera)
        {
            dbContext.Cameras.Add(camera);
            dbContext.SaveChanges();
        }

        public List<Camera>? GetLightAllCameras(string email)
        { 
            User? user = GetHeavyUserFromEmail(email);
            return user?.Cameras?.ToList();
        }

        public Camera? GetCamera(string guid)
        {
            return dbContext.Cameras.FirstOrDefault(a => a.CameraGuid == guid);
        }

    }
}