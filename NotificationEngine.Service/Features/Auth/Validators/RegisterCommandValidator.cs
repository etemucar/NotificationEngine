using FluentValidation;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

/// <summary>
/// RegisterCommandHandler'ın kendi varsayımlarıyla uyumlu: handler, Identifier'ı '@' içeriyorsa
/// email, içermiyorsa telefon sayar — validator da aynı ayrımı yapar.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.GivenName)
            .NotEmpty().WithMessage("Ad boş olamaz.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.FamilyName)
            .NotEmpty().WithMessage("Soyad boş olamaz.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Email veya telefon boş olamaz.")
            .MaximumLength(254).WithMessage("Email veya telefon en fazla 254 karakter olabilir.");

        When(x => !string.IsNullOrEmpty(x.Identifier) && x.Identifier.Contains('@'), () =>
        {
            RuleFor(x => x.Identifier)
                .EmailAddress().WithMessage("Geçerli bir email adresi girin.");
        });

        // Telefon: yalnızca rakamlar, isteğe bağlı baştaki '+' (boşluk/tire/parantez kabul edilmez —
        // istemci bunları temizleyip göndermeli; handler değeri olduğu gibi saklıyor ve login'de
        // birebir karşılaştırıyor).
        When(x => !string.IsNullOrEmpty(x.Identifier) && !x.Identifier.Contains('@'), () =>
        {
            RuleFor(x => x.Identifier)
                .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Geçerli bir telefon numarası girin (yalnızca rakam, isteğe bağlı baştaki +).");
        });

        RuleFor(x => x.Password).MustBeValidPassword();

        RuleFor(x => x.LanguageId)
            .GreaterThan(0).WithMessage("Geçerli bir dil seçin.");
    }
}
