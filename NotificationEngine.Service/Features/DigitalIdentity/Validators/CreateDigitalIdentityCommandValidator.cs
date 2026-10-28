using FluentValidation;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public class CreateDigitalIdentityCommandValidator : AbstractValidator<CreateDigitalIdentityCommand>
{
    public CreateDigitalIdentityCommandValidator()
    {
        RuleFor(x => x.Nickname)
            .MaximumLength(100)
            .When(x => x.Nickname is not null);

        RuleFor(x => x.PartyRoleId)
            .GreaterThan(0);

        RuleFor(x => x.Credentials)
            .NotEmpty()
            .WithMessage("En az bir Credential gönderilmelidir.");

        RuleForEach(x => x.Credentials)
            .SetValidator(new CredentialRequestValidator());
    }
}

public class CredentialRequestValidator : AbstractValidator<CredentialRequest>
{
    public CredentialRequestValidator()
    {
        RuleFor(x => x.CredentialType)
            .IsInEnum();

        RuleFor(x => x.TrustLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TrustLevel.HasValue);

        RuleFor(x => x.Characteristics)
            .NotEmpty()
            .WithMessage("Credential en az bir characteristic içermelidir.");

        RuleForEach(x => x.Characteristics).ChildRules(c =>
        {
            c.RuleFor(ch => ch.Name).NotEmpty();
            c.RuleFor(ch => ch.Value).NotEmpty();
        });

        // CredentialType.Password ise "password" adlı bir characteristic zorunlu
        // (handler'da bu isimle arayıp hash'liyor — bkz. CreateDigitalIdentityCommandHandler)
        RuleFor(x => x)
            .Must(cr => cr.Characteristics.Any(ch => ch.Name == "password"))
            .WithMessage("CredentialType Password ise 'password' adlı bir characteristic gönderilmelidir.")
            .When(x => x.CredentialType == CredentialType.Password);

        RuleForEach(x => x.ContactMedia)
            .SetValidator(new ContactMediumRequestValidator());
    }
}

public class ContactMediumRequestValidator : AbstractValidator<ContactMediumRequest>
{
    private static readonly HashSet<string> ValidMediumTypes =
        Enum.GetNames(typeof(ContactMediumType)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public ContactMediumRequestValidator()
    {
        // Handler'da Enum.Parse<ContactMediumType>(cm.MediumType, ignoreCase: true) yapılıyor
        // ve try/catch YOK — geçersiz bir değer FormatException fırlatıp 500'e düşer.
        // Bu kural o durumu erken (400 olarak) yakalıyor.
        RuleFor(x => x.MediumType)
            .NotEmpty()
            .Must(mt => ValidMediumTypes.Contains(mt))
            .WithMessage(x => $"Geçersiz MediumType: '{x.MediumType}'. Geçerli değerler: {string.Join(", ", ValidMediumTypes)}");

        RuleFor(x => x.Characteristic)
            .Must(HasKey("emailAddress"))
            .When(x => string.Equals(x.MediumType, nameof(ContactMediumType.EmailAddress), StringComparison.OrdinalIgnoreCase))
            .WithMessage("EmailAddress için Characteristic içinde 'emailAddress' anahtarı zorunludur.");

        RuleFor(x => x.Characteristic)
            .Must(HasKey("phoneNumber"))
            .When(x => string.Equals(x.MediumType, nameof(ContactMediumType.PhoneNumber), StringComparison.OrdinalIgnoreCase))
            .WithMessage("PhoneNumber için Characteristic içinde 'phoneNumber' anahtarı zorunludur.");
    }

    private static Func<IDictionary<string, object>, bool> HasKey(string key) => characteristic =>
        characteristic is not null &&
        characteristic.TryGetValue(key, out var v) &&
        !string.IsNullOrWhiteSpace(v?.ToString());
}
