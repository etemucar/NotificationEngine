using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class CreatePartyRoleCommand : IRequest<PartyRoleResponse>, ITransactionalRequest
{
    public int    PartyId         { get; set; }
    public int PartyRoleTypeId { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
}

public class PatchPartyRoleCommand : IRequest<PartyRoleResponse>, ITransactionalRequest
{
    public int     Id              { get; set; }
    public int? PartyRoleTypeId { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
}
 
public class DeletePartyRoleCommand : IRequest<bool>, ITransactionalRequest
{
    public int Id { get; set; }
}
