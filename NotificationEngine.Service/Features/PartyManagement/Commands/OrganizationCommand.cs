using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class CreateOrganizationCommand : IRequest<OrganizationResponse>, ITransactionalRequest
{
    public string  Name                { get; set; } = null!;
    public string? TaxOffice           { get; set; }
    public long    TaxNumber           { get; set; }
    public long    IdentityNumber      { get; set; }
    public string? TradeName           { get; set; }
    public long    TradeRegisterNumber { get; set; }
    public long    MersisNo            { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
    public List<ContactMediumRequest>  ContactMedium { get; set; } = new();
    public List<RelatedPartyRequest>   RelatedParty  { get; set; } = new();
}

public class PatchOrganizationCommand : IRequest<OrganizationResponse>, ITransactionalRequest
{
    public int     Id                   { get; set; }
    public string? Name                 { get; set; }
    public string? TaxOffice            { get; set; }
    public long?   TaxNumber            { get; set; }
    public long?   IdentityNumber       { get; set; }
    public string? TradeName            { get; set; }
    public long?   TradeRegisterNumber  { get; set; }
    public long?   MersisNo             { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
}
 
public class DeleteOrganizationCommand : IRequest<bool>, ITransactionalRequest
{
    public int Id { get; set; }
}
