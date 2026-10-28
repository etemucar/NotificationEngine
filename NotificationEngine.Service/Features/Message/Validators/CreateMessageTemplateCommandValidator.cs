using FluentValidation;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public sealed class CreateMessageTemplateCommandValidator : AbstractValidator<CreateMessageTemplateCommand>
{
    public CreateMessageTemplateCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[A-Z0-9_]+$")
            .WithMessage("Kod yalnızca büyük harf, rakam ve alt çizgi içermeli (örn. PASSWORD_RESET).");

        RuleFor(x => x.Channel).IsInEnum();

        RuleFor(x => x.Culture)
            .NotEmpty()
            .Matches(@"^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Kültür 'tr-TR' biçiminde olmalı.");

        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Subject)
            .NotEmpty()
            .When(x => x.Channel == Channel.Email)
            .WithMessage("E-posta şablonu için konu gerekli.");

        RuleFor(x => x.Body).NotEmpty();
        When(x => x.Channel == Channel.Email, () => RuleFor(x => x.Body).MaximumLength(20_000));
        When(x => x.Channel != Channel.Email, () => RuleFor(x => x.Body).MaximumLength(1_000));
    }
}