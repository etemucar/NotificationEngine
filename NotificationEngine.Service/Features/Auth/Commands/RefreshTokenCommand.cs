using MediatR;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class RefreshTokenCommand : IRequest<AuthResult>, ITransactionalRequest
{
    public string RefreshToken { get; set; } = null!;
}
