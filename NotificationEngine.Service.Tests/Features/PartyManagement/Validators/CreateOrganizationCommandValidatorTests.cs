using FluentAssertions;
using FluentValidation;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Validators;

namespace NotificationEngine.Service.Tests.Features.Validators;

public class CreateOrganizationCommandValidatorTests
{
    private readonly CreateOrganizationCommandValidator _validator = new();

    // ── Geçerli senaryolar ──────────────────────────────────────────────

    [Fact]
    public async Task Validate_WithTaxOfficeAndTaxNumber_ShouldBeValid()
    {
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithIdentityNumber_ShouldBeValid()
    {
        var command = new CreateOrganizationCommand
        {
            Name           = "Test A.Ş.",
            IdentityNumber = 12345678901
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    // ── Geçersiz senaryolar ─────────────────────────────────────────────

    [Fact]
    public async Task Validate_WithoutName_ShouldBeInvalid()
    {
        var command = new CreateOrganizationCommand
        {
            Name      = "",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithTaxNumberButNoTaxOffice_ShouldBeInvalid()
    {
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "",
            TaxNumber = 1234567890
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fatura Bilgisi");
    }

    [Fact]
    public async Task Validate_WithTaxOfficeButNoTaxNumber_ShouldBeInvalid()
    {
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 0
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fatura Bilgisi");
    }

    [Fact]
    public async Task Validate_WithNoInvoiceInfo_ShouldBeInvalid()
    {
        var command = new CreateOrganizationCommand
        {
            Name           = "Test A.Ş.",
            TaxOffice      = "",
            TaxNumber      = 0,
            IdentityNumber = 0
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fatura Bilgisi");
    }

    [Fact]
    public async Task Validate_NameExceeds200Characters_ShouldBeInvalid()
    {
        var command = new CreateOrganizationCommand
        {
            Name      = new string('A', 201),
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
