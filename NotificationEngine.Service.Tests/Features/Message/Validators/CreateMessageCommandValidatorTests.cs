using FluentAssertions;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class CreateMessageCommandValidatorTests
{
    private readonly CreateMessageCommandValidator _validator = new();

    private static CreateMessageCommand ValidSms() => new(
        Channel.Sms, "+905551112233", null, "Kodunuz: 1234", null, null,
        MessagePriority.Normal, null, null, "key-1");

    private static CreateMessageCommand ValidEmail() => new(
        Channel.Email, "ali@site.com", "Konu", "Gövde", null, null,
        MessagePriority.Normal, null, null, "key-1");

    // ── Geçerli ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ValidSmsWithContent_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(ValidSms(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidEmailWithContentAndSubject_ShouldBeValid()
    {
        var result = await _validator.ValidateAsync(ValidEmail(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_TemplateModeWithoutContent_ShouldBeValid()
    {
        var command = ValidSms() with { Content = null, TemplateCode = "OTP_PASSWORD" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmailInTemplateMode_DoesNotRequireSubject()
    {
        // Konu şablondan gelir; handler kontrol eder.
        var command = ValidEmail() with { Content = null, Subject = null, TemplateCode = "WELCOME" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    // ── Geçersiz ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("905551112233")]      // '+' yok
    [InlineData("+05551112233")]      // ülke kodu 0 ile başlıyor
    [InlineData("+90555")]            // çok kısa
    [InlineData("+90 555 111 22 33")] // boşluk
    [InlineData("abc")]
    public async Task Validate_SmsWithBadPhoneNumber_ShouldBeInvalid(string phone)
    {
        var command = ValidSms() with { Recipient = phone };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_EmailWithBadAddress_ShouldBeInvalid()
    {
        var command = ValidEmail() with { Recipient = "ali-at-site" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public async Task Validate_NeitherContentNorTemplate_ShouldBeInvalid()
    {
        var command = ValidSms() with { Content = null, TemplateCode = null };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmailContentModeWithoutSubject_ShouldBeInvalid()
    {
        var command = ValidEmail() with { Subject = null };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task Validate_MissingIdempotencyKey_ShouldBeInvalid()
    {
        var command = ValidSms() with { IdempotencyKey = "" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public async Task Validate_MoreThan50Parameters_ShouldBeInvalid()
    {
        var parameters = Enumerable.Range(0, 51).ToDictionary(i => $"p{i}", i => "v");
        var command = ValidSms() with { Content = null, TemplateCode = "T", Parameters = parameters };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ContentLongerThan10000_ShouldBeInvalid()
    {
        var command = ValidSms() with { Content = new string('a', 10_001) };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }
}