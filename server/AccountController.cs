using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;

namespace SecurityCameraServer
{
    public class AccountController
    {

        public string CreateUser(string email, string name, string password, string confpassword)
        {
            (string, bool) userCredValidation = EmailPassValid(email, password, confpassword);
            if (userCredValidation.Item2)
            {
                User user = new()
                {
                    EMail = email,
                    PasswordHashData = new SecureHash<SHA256>(password),
                    Name = name,
                };
                Application.Database.AddUser(user);
            }
            return userCredValidation.Item1;
        }

        private (string, bool) EmailPassValid(string email, string password, string confpassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ("Email field is blank", false);
            if (string.IsNullOrWhiteSpace(password))
                return ("Password field is blank", false);
            if (string.IsNullOrWhiteSpace(confpassword))
                return ("Confirm Password field is blank", false);
            if (Application.Database.GetLightUserFromEmail(email) != null)
                return ("Email already exists", false);

            //  This will test if the entered email is an actual email or not
            bool validEMail = true;
            try { MailAddress address = new MailAddress(email); }
            catch { validEMail = false; }

            if (!validEMail)
                return ("Couldn't verify email address", false);
            if (!password.Equals(confpassword))
                return ("Passwords do not match each other", false);
            if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsNumber))
                return ("Password must be at least 8 characters long with at least one uppercase letter, lowercase letter, and number", false);
            return ("Email and Password are Valid", true);
        }

        public bool ChangeName(string email, string authentication, string newName)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;

            user.Name = newName;
            Application.Database.Context.SaveChanges();
            
            return true;
        }

        public bool ResetPassword(string email, string password, string confpassword)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;

            Console.WriteLine(user.EMail);

            if(string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confpassword)) return false;
            if(!password.Equals(confpassword)) return false;
            if(password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsNumber)) return false;

            user.PasswordHashData = new SecureHash<SHA256>(password);
            Application.Database.Context.SaveChanges(); 

            return true;
        }
    }
}
