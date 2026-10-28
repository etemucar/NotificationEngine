using FluentValidation;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public sealed class UpdateMessageTemplateCommandValidator : AbstractValidator<UpdateMessageTemplateCommand>
{
    public UpdateMessageTemplateCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(20_000);
        RuleFor(x => x.Subject).MaximumLength(200);
    }
}