using NotificationEngine.Service.Features.Commands;
using FluentValidation;

namespace NotificationEngine.Service.Features.Validators;

public class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.GivenName).NotEmpty();
        RuleFor(x => x.FamilyName).NotEmpty();
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
