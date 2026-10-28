namespace NotificationEngine.Api.Models;

public class AuthResponse
{
    public bool   Success        { get; set; }
    public int    UserId         { get; set; }
    public string UserName       { get; set; } = null!;
    public string UserIdentifier { get; set; } = null!;
}
