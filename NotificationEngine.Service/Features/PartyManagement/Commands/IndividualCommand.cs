using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class CreateIndividualCommand : IRequest<IndividualResponse>, ITransactionalRequest
{
    public string GivenName      { get; set; } = null!;
    public string FamilyName     { get; set; } = null!;
    public string? MiddleName     { get; set; }
    public string? Title          { get; set; }
    public string? Gender         { get; set; }
    public string? Nationality    { get; set; }
    public DateTime? BirthDate   { get; set; }
    public string? PlaceOfBirth   { get; set; }
    public string? CountryOfBirth { get; set; }
    public string? MaritalStatus  { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
    public List<ContactMediumRequest> ContactMedium { get; set; } = new();
    public List<RelatedPartyRequest>  RelatedParty  { get; set; } = new();
}

public class PatchIndividualCommand : IRequest<IndividualResponse>, ITransactionalRequest
{
    public int    Id              { get; set; }
    public string? GivenName     { get; set; }
    public string? FamilyName    { get; set; }
    public string? MiddleName    { get; set; }
    public string? Title         { get; set; }
    public string? Gender        { get; set; }
    public string? Nationality   { get; set; }
    public DateTime? BirthDate   { get; set; }
    public string? PlaceOfBirth  { get; set; }
    public string? CountryOfBirth { get; set; }
    public string? MaritalStatus { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }
    public List<ContactMediumRequest>? ContactMedium { get; set; }
}
 
public class DeleteIndividualCommand : IRequest<bool>, ITransactionalRequest
{
    public int Id { get; set; }
}
