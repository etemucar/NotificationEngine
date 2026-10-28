using FluentValidation;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public class UpdateDigitalIdentityStatusCommandValidator
    : AbstractValidator<UpdateDigitalIdentityStatusCommand>
{
    public UpdateDigitalIdentityStatusCommandValidator()
    {
        RuleFor(x => x.DigitalIdentityId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
