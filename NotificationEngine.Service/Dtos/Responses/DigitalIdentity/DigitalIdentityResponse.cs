using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Responses;

public class DigitalIdentityResponse
{
    public Guid Id { get; set; }
    public string? Nickname { get; set; }
    public GeneralStatus Status { get; set; }
    public DateTime DigitalIdentityDate { get; set; }
    public int PartyRoleId { get; set; }
}
