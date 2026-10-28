using Moq;
using FluentAssertions;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IRepository<ApplicationUser, int>> _userRepositoryMock;
    private readonly GetCurrentUserQueryHandler               _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepository<ApplicationUser, int>>();
        _handler            = new GetCurrentUserQueryHandler(_userRepositoryMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ApplicationUser BuildUser(
        int userId        = 1,
        string email      = "john@example.com",
        string givenName  = "John",
        string familyName = "Doe",
        string languageCd = "tr",
        bool   withIndividual = true)
    {
        return new ApplicationUser
        {
            Id       = userId,
            Language = new Language { Id = 1, LanguageCd = languageCd, Name = "Türkçe" },
            DigitalIdentity = new DigitalIdentity
            {
                Id       = Guid.NewGuid(),
                Nickname = "john_doe",
                PartyRole = new PartyRole
                {
                    Id      = 1,
                    PartyId = 10,
                    Party = withIndividual
                        ? new Party
                          {
                              Individual = new Individual
                              {
                                  GivenName  = givenName,
                                  FamilyName = familyName
                              }
                          }
                        : new Party()
                },
                Credentials = new List<Credential>
                {
                    new()
                    {
                        CredentialType = CredentialType.Password,
                        ContactMedia = new List<ContactMedium>
                        {
                            new()
                            {
                                MediumType  = ContactMediumType.EmailAddress,
                                IsPreferred = true,
                                Email       = email
                            }
                        }
                    }
                }
            }
        };
    }

    private void SetupUser(ApplicationUser? user)
    {
        _userRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingUser_ShouldReturnCurrentUserResponse()
    {
        // Arrange
        SetupUser(BuildUser(userId: 1, email: "john@example.com", givenName: "John", familyName: "Doe"));

        // Act
        var result = await _handler.Handle(
            new GetCurrentUserQuery(1), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Identifier.Should().Be("john@example.com");
        result.GivenName.Should().Be("John");
        result.FamilyName.Should().Be("Doe");
        result.Locale.Should().Be("tr");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        SetupUser(null);

        // Act
        var act = async () => await _handler.Handle(
            new GetCurrentUserQuery(999), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*bulunamadı*");
    }

    [Fact]
    public async Task Handle_IndividualIsNull_ShouldReturnEmptyNames()
    {
        // Arrange — Party.Individual set edilmemişse (teorik olarak olmamalı ama
        // handler null-conditional ile ele alıyor) boş string dönmeli, exception değil.
        SetupUser(BuildUser(withIndividual: false));

        // Act
        var result = await _handler.Handle(
            new GetCurrentUserQuery(1), TestContext.Current.CancellationToken);

        // Assert
        result.GivenName.Should().BeEmpty();
        result.FamilyName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PhoneOnlyContactMedium_ShouldReturnPhoneAsIdentifier()
    {
        // Arrange
        var user = BuildUser();
        user.DigitalIdentity.Credentials.First().ContactMedia = new List<ContactMedium>
        {
            new()
            {
                MediumType  = ContactMediumType.PhoneNumber,
                IsPreferred = true,
                PhoneNumber = "05551234567"
            }
        };
        SetupUser(user);

        // Act
        var result = await _handler.Handle(
            new GetCurrentUserQuery(1), TestContext.Current.CancellationToken);

        // Assert
        result.Identifier.Should().Be("05551234567");
    }

    [Fact]
    public async Task Handle_DifferentLanguage_ShouldReturnCorrectLocale()
    {
        // Arrange
        SetupUser(BuildUser(languageCd: "en"));

        // Act
        var result = await _handler.Handle(
            new GetCurrentUserQuery(1), TestContext.Current.CancellationToken);

        // Assert
        result.Locale.Should().Be("en");
    }

    [Fact]
    public async Task Handle_ShouldQueryByRequestedUserId()
    {
        // Arrange
        Expression<Func<ApplicationUser, bool>>? capturedPredicate = null;

        _userRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<ApplicationUser, bool>>,
                    Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>?,
                    bool, CancellationToken>((predicate, _, _, _) => capturedPredicate = predicate)
            .ReturnsAsync(BuildUser(userId: 42));

        // Act
        await _handler.Handle(new GetCurrentUserQuery(42), TestContext.Current.CancellationToken);

        // Assert — predicate'in 42 Id'li kullanıcıyla eşleştiğini, 43 ile eşleşmediğini doğrula
        capturedPredicate.Should().NotBeNull();
        var compiled = capturedPredicate!.Compile();
        compiled(new ApplicationUser { Id = 42 }).Should().BeTrue();
        compiled(new ApplicationUser { Id = 43 }).Should().BeFalse();
    }

}
