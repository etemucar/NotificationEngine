namespace NotificationEngine.Service.Dtos.Responses;

public class AuthResult
{
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string UserIdentifier { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenExpiration { get; set; }
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshTokenExpiration { get; set; }
}
