namespace NotificationEngine.Service.Dtos.Responses;

public class CurrentUserResponse
{
    public int    Id         { get; set; }
    public string Identifier { get; set; } = null!;
    public string GivenName  { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string Locale     { get; set; } = null!;
}
