namespace NotificationEngine.Service.Dtos.Responses;

public class PartyRoleResponse
{
    public int    Id              { get; set; }
    public int    PartyId         { get; set; }
    public int PartyRoleTypeId { get; set; }
    public TimePeriodResponse ValidFor { get; set; } = new();
}
