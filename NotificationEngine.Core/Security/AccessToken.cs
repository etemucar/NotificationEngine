using System;

namespace NotificationEngine.Core.Security
{
    public class TokenUser
    {
        public string UserName { get; set; }= string.Empty;
        public int UserId { get; set; }
        public string UserCredential { get; set; }= string.Empty;

        public TokenUser (string userName, int userId, string userCredential)
        {
            UserName = userName;
            UserId = userId;
            UserCredential = userCredential;
        }
    }

    public class AccessToken
    {
        public TokenUser? User { get; set; }
        public string Token { get; set; }= string.Empty;
        public DateTime Expiration { get; set; }
    }
}
