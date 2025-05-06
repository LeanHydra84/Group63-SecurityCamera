using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;

namespace SecurityCameraServer
{
    public class AccountController
    {

        public bool CreateUser(string email, string name, string password)
        {
            if (Application.Database.GetLightUserFromEmail(email) == null)
            {
                User user = new()
                {
                    EMail = email,
                    PasswordHashData = new SecureHash<SHA256>(password),
                    Name = name,
                };
                Application.Database.AddUser(user);
                return true;
            }

            return false;
        }

        public bool ChangeName(string email, string authentication, string newName)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;

            user.Name = newName;
            Application.Database.Context.SaveChanges();
            
            return true;
        }

        public bool ResetPassword(string email, string password)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;

            Console.WriteLine(user.EMail);

            if(string.IsNullOrEmpty(password)) return false;
            if (password.Length < 5) return false;

            user.PasswordHashData = new SecureHash<SHA256>(password);
            Application.Database.Context.SaveChanges(); 

            return true;
        }
    }
}
