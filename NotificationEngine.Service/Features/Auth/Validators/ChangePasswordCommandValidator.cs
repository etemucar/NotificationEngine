using FluentValidation;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Kullanıcı doğrulanamadı.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre boş olamaz.");

        RuleFor(x => x.NewPassword).MustBeValidPassword();

        // Mevcut şifre handler'da doğrulanıyor; burada yalnızca düz metin karşılaştırması yeterli
        // (CurrentPassword doğruysa NewPassword == CurrentPassword, mevcut şifreyle aynı demektir).
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Yeni şifre mevcut şifreyle aynı olamaz.");
    }
}
