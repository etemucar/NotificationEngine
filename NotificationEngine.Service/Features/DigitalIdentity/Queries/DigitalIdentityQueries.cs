using MediatR;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Queries;

public record GetCurrentUserQuery(int UserId) : IRequest<CurrentUserResponse>;
