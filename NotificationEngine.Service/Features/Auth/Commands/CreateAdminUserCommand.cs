using MediatR;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;
// NOT: ITransactionalRequest'in gerçek namespace'i projenizde neredeyse
// (RegisterCommand'ın using bloğunu görmedim) buna göre düzeltin.

namespace NotificationEngine.Service.Features.Commands;

public class CreateAdminUserCommand : IRequest<CreateAdminUserResult>, ITransactionalRequest
{
    public string GivenName  { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string Identifier { get; set; } = null!; // email veya telefon
    public string Password   { get; set; } = null!;
    public int    LanguageId { get; set; } = 1;
}
