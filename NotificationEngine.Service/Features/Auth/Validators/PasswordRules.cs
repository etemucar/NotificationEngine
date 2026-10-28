using FluentValidation;

namespace NotificationEngine.Service.Features.Validators;

/// <summary>
/// Şifre kuralları TEK yerde: RegisterCommandValidator ve ChangePasswordCommandValidator aynı
/// kuralları kullanır, ikisi birbirinden ayrışmasın.
///
/// MaxLength: IPasswordHasher'ın algoritması burada görülmedi; çok uzun girdiyi hash'e hiç
/// vermemek (CPU maliyeti) için üst sınır var. Hasher BCrypt ise 72 bayttan sonrasını sessizce
/// yok sayar — o durumda MaxLength'i 72'ye çekmek gerekir.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> MustBeValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(MinLength).WithMessage($"Şifre en az {MinLength} karakter olmalı.")
            .MaximumLength(MaxLength).WithMessage($"Şifre en fazla {MaxLength} karakter olabilir.")
            .Matches(@"\p{L}").WithMessage("Şifre en az bir harf içermeli.")
            .Matches(@"\d").WithMessage("Şifre en az bir rakam içermeli.");
}
