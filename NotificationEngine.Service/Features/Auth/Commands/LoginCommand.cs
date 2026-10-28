using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class LoginCommand : IRequest<AuthResult>, ITransactionalRequest
{
    public string Identifier { get; set; } = null!;
    public string Password { get; set; } = null!;
}
