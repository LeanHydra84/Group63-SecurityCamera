using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;

// AUTHENTICATION SYSTEM: "Is the user who they say they are?"
// DIFFERENT THAN THE AUTHORIZATION SYSTEM, which is "Does this user have permission to do this?"

namespace SecurityCameraServer
{
    public class UserSessionHandler
    {

        public TimeSpan AuthenticationTokenExpirationTime { get; } = new(72, 0, 0);

        // Hash map of USER-ID to LoginSession
        private Dictionary<int, LoginSession> activeSessions = new();

        private bool HasExpired(LoginSession session) => session.Expires < DateTime.Now;

        // private User? GetUserFromToken(string auth)
        // {
        //     if(activeSessions.TryGetValue(auth, out LoginSession? session))
        //     {
        //         if (HasExpired(session))
        //         {
        //             activeSessions.Remove(auth);
        //             return null;
        //         }
        //         return session?.User;
        //     }
        //     return null;
        // }


        public void CullActiveSessions()
        {
            List<int> removeList = new();
            foreach (KeyValuePair<int, LoginSession> kvp in activeSessions)
            {
                if (HasExpired(kvp.Value))
                {
                    removeList.Add(kvp.Key);
                }
            }
            foreach (int id in removeList)
            {
                activeSessions.Remove(id);
            }
        }

        public bool ValidateAuthentication(string email, string authentication)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;
            return ValidateAuthentication(user, authentication);
        }

        public bool ValidateAuthentication(User user, string? authentication)
        {
            if (authentication == null) return false;
            if (!activeSessions.ContainsKey(user.ID)) return false;
            LoginSession session = activeSessions[user.ID];
            if (HasExpired(session)) return false;
            return authentication == session.Authentication;
        }

        public bool DoesUserHaveActiveSession(User user)
        {
            if (!activeSessions.ContainsKey(user.ID)) return false;
            LoginSession session = activeSessions[user.ID];
            if (HasExpired(session))
            {
                activeSessions.Remove(user.ID);
                return false;
            }

            return true;
        }

        private string NewGUID128String()
        {
            // const int generatorDataLength = 32;
            // const string alnum = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            // string data = new string(Enumerable.Repeat(alnum, generatorDataLength)
            //     .Select(a => a[Random.Shared.Next(alnum.Length)])
            //     .ToArray());
            // return new SecureHash<SHA256>(data).ToString();
            string authToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return authToken;
        }

        public string? Login(string email, string password)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return null;

            var suppliedHash = new SecureHash<SHA256>(password);
            var realHash = user.PasswordHashData;
            if (ReferenceEquals(realHash, null))
                throw new Exception("User password hash is null here. This should never be null.");
            if (suppliedHash == realHash)
            {
                if (DoesUserHaveActiveSession(user))
                {
                    return activeSessions[user.ID].Authentication;
                }

                string authcode = NewGUID128String();
                LoginSession session = new LoginSession
                {
                    Expires = DateTime.Now + AuthenticationTokenExpirationTime,
                    User = user,
                    Authentication = authcode,
                };
                activeSessions[user.ID] = session;

                return authcode;
            }

            return null;

        }

        public bool Logout(string email)
        {
            User? user = Application.Database.GetLightUserFromEmail(email);
            if (user == null) return false;
            if(activeSessions.ContainsKey(user.ID))
            {
                activeSessions.Remove(user.ID);
                return true;
            }
            return false;
        }

    }
}