namespace NotificationEngine.Service.Dtos.Responses;

public class CreateAdminUserResult
{
    public int    UserId         { get; set; }
    public string UserName       { get; set; } = null!;
    public string UserIdentifier { get; set; } = null!;
}
