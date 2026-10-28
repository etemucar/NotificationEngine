using MediatR;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Queries;

public class GetPartyRoleQuery : IRequest<PartyRoleResponse>
{
    public int Id { get; set; }
}

public class GetPartyRoleListQuery : IRequest<IEnumerable<PartyRoleResponse>>
{
    public int    Offset  { get; set; } = 0;
    public int    Limit   { get; set; } = 20;
    public string? Fields { get; set; }
}
