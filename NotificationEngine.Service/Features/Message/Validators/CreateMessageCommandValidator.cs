using FluentValidation;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public sealed class CreateMessageCommandValidator : AbstractValidator<CreateMessageCommand>
{
    private const string E164 = @"^\+[1-9]\d{7,14}$";

    public CreateMessageCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(320);
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();

        When(x => x.Channel == Channel.Email, () =>
            RuleFor(x => x.Recipient).EmailAddress());

        When(x => x.Channel == Channel.Sms, () =>
            RuleFor(x => x.Recipient).Matches(E164)
                .WithMessage("Telefon numarası E.164 biçiminde olmalı (örn. +905551112233)."));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.TemplateCode))
            .WithMessage("'content' ya da 'templateCode' verilmeli.");

        // Content modunda e-posta için konu zorunlu (şablon modunda konu şablondan gelir, handler kontrol eder)
        When(x => !string.IsNullOrWhiteSpace(x.Content) && x.Channel == Core.Enums.Channel.Email, () =>
            RuleFor(x => x.Subject).NotEmpty().WithMessage("E-posta için 'subject' gerekli."));

        RuleFor(x => x.Content).MaximumLength(10_000);
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.TemplateCode).MaximumLength(100);
        RuleFor(x => x.Culture).MaximumLength(10);

        RuleFor(x => x.Parameters)
            .Must(p => p is null || p.Count <= 50)
            .WithMessage("En fazla 50 şablon parametresi gönderilebilir.");
    }
}