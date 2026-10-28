using MediatR;
using NotificationEngine.Service.Dtos.Responses;
 
namespace NotificationEngine.Service.Features.Queries;
 
public class GetOrganizationQuery : IRequest<OrganizationResponse>
{
    public int Id { get; set; }
}
 
public class GetOrganizationListQuery : IRequest<IEnumerable<OrganizationResponse>>
{
    public int    Offset  { get; set; } = 0;
    public int    Limit   { get; set; } = 20;
    public string? Fields { get; set; }
}
 
