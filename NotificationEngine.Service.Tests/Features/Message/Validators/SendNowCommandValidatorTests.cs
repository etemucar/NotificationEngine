using FluentAssertions;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class SendNowCommandValidatorTests
{
    private readonly SendNowCommandValidator _validator = new();

    private static SendNowCommand Valid() => new(
        Channel.Sms, "+905551112233", null, "Kodunuz: 1234", null, null, null);

    private static SendNowCommand ValidEmail() => new(
        Channel.Email, "kullanici@example.com", "Şifre sıfırlama", "Bağlantı: https://example.com/reset", null, null, null);

    [Fact]
    public async Task Validate_ValidSms_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(Valid(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidEmail_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(ValidEmail(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmailWithTemplateAndNoSubject_ShouldBeValid()
    {
        // Konu şablondan gelir; bu kontrol handler'da yapılır.
        var command = ValidEmail() with { Subject = null, Content = null, TemplateCode = "PASSWORD_RESET" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_PushChannel_ShouldBeInvalid()
    {
        var command = Valid() with { Channel = Channel.Push };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public async Task Validate_BadPhone_ShouldBeInvalid()
    {
        var command = Valid() with { Recipient = "5551112233" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_SmsWithEmailRecipient_ShouldBeInvalid()
    {
        var command = Valid() with { Recipient = "kullanici@example.com" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_BadEmail_ShouldBeInvalid()
    {
        var command = ValidEmail() with { Recipient = "gecersiz-adres" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_EmailWithPhoneRecipient_ShouldBeInvalid()
    {
        var command = ValidEmail() with { Recipient = "+905551112233" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_EmailWithContentButNoSubject_ShouldBeInvalid()
    {
        var command = ValidEmail() with { Subject = null };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task Validate_NeitherContentNorTemplate_ShouldBeInvalid()
    {
        var command = Valid() with { Content = null, TemplateCode = null };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_SmsContentLongerThan1000_ShouldBeInvalid()
    {
        var command = Valid() with { Content = new string('a', 1_001) };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }
}