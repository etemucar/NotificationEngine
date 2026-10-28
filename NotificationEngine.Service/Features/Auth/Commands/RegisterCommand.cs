using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;


public class RegisterCommand : IRequest<AuthResult>, ITransactionalRequest
{
    public string GivenName    { get; set; } = null!;
    public string FamilyName   { get; set; } = null!;
    public string Identifier   { get; set; } = null!; // email veya telefon
    public string Password     { get; set; } = null!;
    public int    LanguageId   { get; set; }
}
