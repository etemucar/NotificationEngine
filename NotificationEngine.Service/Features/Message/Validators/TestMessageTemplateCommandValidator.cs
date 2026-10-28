using FluentValidation;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Validators;

public sealed class TestMessageTemplateCommandValidator : AbstractValidator<TestMessageTemplateCommand>
{
    public TestMessageTemplateCommandValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(254);

        RuleFor(x => x.Parameters)
            .Must(p => p is null || p.Count <= 50)
            .WithMessage("En fazla 50 şablon parametresi gönderilebilir.");

        RuleFor(x => x.Parameters)
            .Must(p => p is null || p.Values.All(v => v is null || v.Length <= 1_000))
            .WithMessage("Parametre değerleri en fazla 1.000 karakter olabilir.");
    }
}