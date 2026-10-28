using FluentValidation;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public sealed class SendNowCommandValidator : AbstractValidator<SendNowCommand>
{
    private const string E164 = @"^\+[1-9]\d{7,14}$";

    public SendNowCommandValidator()
    {
        // Anında gönderim: şifre belirleme/sıfırlama gibi içeriği saklanmayan mesajlar. Push anlamlı değil.
        RuleFor(x => x.Channel)
            .Must(c => c is Channel.Sms or Channel.Email)
            .WithMessage("Anında gönderim yalnızca SMS ve e-posta kanalları için desteklenir.");

        When(x => x.Channel == Channel.Sms, () =>
        {
            RuleFor(x => x.Recipient).NotEmpty();
            RuleFor(x => x.Recipient).Matches(E164)
                .WithMessage("Telefon numarası E.164 biçiminde olmalı (örn. +905551112233).");
            RuleFor(x => x.Content).MaximumLength(1_000);
        });

        When(x => x.Channel == Channel.Email, () =>
        {
            RuleFor(x => x.Recipient).NotEmpty();
            RuleFor(x => x.Recipient).EmailAddress().MaximumLength(254)
                .WithMessage("Geçerli bir e-posta adresi olmalı.");
            RuleFor(x => x.Subject).MaximumLength(200);
            RuleFor(x => x.Subject).NotEmpty()
                .When(x => !string.IsNullOrWhiteSpace(x.Content))
                .WithMessage("Hazır içerikle e-posta için 'subject' gerekli.");
            RuleFor(x => x.Content).MaximumLength(20_000);
        });

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.TemplateCode))
            .WithMessage("'content' ya da 'templateCode' verilmeli.");

        RuleFor(x => x.TemplateCode).MaximumLength(100);
        RuleFor(x => x.Culture).MaximumLength(10);
        RuleFor(x => x.Parameters)
            .Must(p => p is null || p.Count <= 50)
            .WithMessage("En fazla 50 şablon parametresi gönderilebilir.");
    }
}