using FluentAssertions;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class CreateMessageTemplateCommandValidatorTests
{
    private readonly CreateMessageTemplateCommandValidator _validator = new();

    private static CreateMessageTemplateCommand Valid() => new(
        OrganizationId: null, Code: "PASSWORD_RESET", Channel: Channel.Sms,
        Culture: "tr-TR", Subject: null, Body: "Kodunuz: {{code}}");

    [Fact]
    public async Task Validate_ValidSms_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(Valid(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidEmailWithSubject_ShouldBeValid()
    {
        var command = Valid() with { Channel = Channel.Email, Subject = "Konu", Body = new string('a', 5_000) };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmailWithoutSubject_ShouldBeInvalid()
    {
        var command = Valid() with { Channel = Channel.Email, Subject = null };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Theory]
    [InlineData("password_reset")]
    [InlineData("PASSWORD-RESET")]
    [InlineData("SIFRE SIFIRLA")]
    [InlineData("")]
    public async Task Validate_BadCode_ShouldBeInvalid(string code)
    {
        var command = Valid() with { Code = code };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("tr_TR")]
    [InlineData("TR-tr")]
    [InlineData("")]
    public async Task Validate_BadCulture_ShouldBeInvalid(string culture)
    {
        var command = Valid() with { Culture = culture };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Culture");
    }

    [Fact]
    public async Task Validate_EmptyBody_ShouldBeInvalid()
    {
        var command = Valid() with { Body = " " };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Theory]
    [InlineData(Channel.Sms)]
    [InlineData(Channel.Push)]
    public async Task Validate_SmsOrPushBodyLongerThan1000_ShouldBeInvalid(Channel channel)
    {
        var command = Valid() with { Channel = channel, Body = new string('a', 1_001) };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public async Task Validate_EmailBodyLongerThan20000_ShouldBeInvalid()
    {
        var command = Valid() with { Channel = Channel.Email, Subject = "Konu", Body = new string('a', 20_001) };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }
}