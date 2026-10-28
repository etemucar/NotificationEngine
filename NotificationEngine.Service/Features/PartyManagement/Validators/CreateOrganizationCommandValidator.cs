using FluentValidation;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kurum adı zorunludur.")
            .MaximumLength(200).WithMessage("Kurum adı 200 karakteri geçemez.");

        // Fatura kuralı: (TaxOffice VE TaxNumber) VEYA IdentityNumber
        RuleFor(x => x)
            .Must(x =>
                (x.TaxNumber > 0 && !string.IsNullOrWhiteSpace(x.TaxOffice)) ||
                x.IdentityNumber > 0)
            .WithName("Fatura Bilgisi")
            .WithMessage("Fatura bilgisi için vergi dairesi ve vergi numarası ya da TC kimlik numarası girilmelidir.");
    }
}
