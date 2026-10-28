using FluentAssertions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class TestMessageTemplateCommandValidatorTests
{
    private readonly TestMessageTemplateCommandValidator _validator = new();

    private static TestMessageTemplateCommand Valid() => new(
        Id: 1, OrganizationId: 7, Recipient: "+905551112233",
        Parameters: new Dictionary<string, string> { ["code"] = "123456" });

    [Fact]
    public async Task Validate_Valid_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(Valid(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullParameters_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(
            Valid() with { Parameters = null }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyRecipient_ShouldBeInvalid()
    {
        var result = await _validator.ValidateAsync(
            Valid() with { Recipient = "" }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_MoreThan50Parameters_ShouldBeInvalid()
    {
        var parameters = Enumerable.Range(0, 51).ToDictionary(i => $"p{i}", _ => "x");

        var result = await _validator.ValidateAsync(
            Valid() with { Parameters = parameters }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ParameterValueLongerThan1000_ShouldBeInvalid()
    {
        var parameters = new Dictionary<string, string> { ["code"] = new string('a', 1_001) };

        var result = await _validator.ValidateAsync(
            Valid() with { Parameters = parameters }, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }
}