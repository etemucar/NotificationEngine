namespace NotificationEngine.Core.Security
{
    public class TokenOptions
    {
        public string Audience { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public int AccessTokenExpiration { get; set; }
        public int RefreshTokenExpiration { get; set; }
        public string SecurityKey { get; set; } = string.Empty;
        public string RefreshSecurityKey { get; set; } = string.Empty;
    }
}
