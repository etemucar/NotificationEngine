using FluentAssertions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class UpdateMessageTemplateCommandValidatorTests
{
    private readonly UpdateMessageTemplateCommandValidator _validator = new();

    private static UpdateMessageTemplateCommand Valid() => new(
        Id: 1, OrganizationId: 7, Subject: "Konu", Body: "Yeni gövde");

    [Fact]
    public async Task Validate_Valid_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(Valid(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyBody_ShouldBeInvalid()
    {
        var result = await _validator.ValidateAsync(
            Valid() with { Body = "" }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public async Task Validate_BodyLongerThan20000_ShouldBeInvalid()
    {
        var result = await _validator.ValidateAsync(
            Valid() with { Body = new string('a', 20_001) }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_SubjectLongerThan200_ShouldBeInvalid()
    {
        var result = await _validator.ValidateAsync(
            Valid() with { Subject = new string('a', 201) }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }
}