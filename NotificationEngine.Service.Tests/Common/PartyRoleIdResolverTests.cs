// NotificationEngine.Service.Tests/Common/PartyRoleIdResolverTests.cs
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Common;

namespace NotificationEngine.Service.Tests.Common;

public class PartyRoleIdResolverTests
{
    private readonly Mock<IRepository<ApplicationUser, int>> _applicationUserRepoMock;
    private readonly PartyRoleIdResolver _resolver;

    public PartyRoleIdResolverTests()
    {
        _applicationUserRepoMock = new Mock<IRepository<ApplicationUser, int>>();
        _resolver = new PartyRoleIdResolver(_applicationUserRepoMock.Object);
    }

    private void SetupFindOneAsync(ApplicationUser? result) =>
        _applicationUserRepoMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public async Task ResolvePartyRoleIdAsync_UserWithPartyRole_ShouldReturnPartyRoleId()
    {
        var user = new ApplicationUser
        {
            Id = 5,
            DigitalIdentity = new DigitalIdentity
            {
                PartyRole = new PartyRole { Id = 77 }
            }
        };
        SetupFindOneAsync(user);

        var result = await _resolver.ResolvePartyRoleIdAsync(5, TestContext.Current.CancellationToken);

        result.Should().Be(77);
    }

    [Fact]
    public async Task ResolvePartyRoleIdAsync_UserNotFound_ShouldThrowNotFoundException()
    {
        SetupFindOneAsync(null);

        var act = () => _resolver.ResolvePartyRoleIdAsync(5, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResolvePartyRoleIdAsync_UserWithoutDigitalIdentity_ShouldThrowNotFoundException()
    {
        var user = new ApplicationUser { Id = 5, DigitalIdentity = null! };
        SetupFindOneAsync(user);

        var act = () => _resolver.ResolvePartyRoleIdAsync(5, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResolvePartyRoleIdAsync_DigitalIdentityWithoutPartyRole_ShouldThrowNotFoundException()
    {
        var user = new ApplicationUser
        {
            Id = 5,
            DigitalIdentity = new DigitalIdentity { PartyRole = null! }
        };
        SetupFindOneAsync(user);

        var act = () => _resolver.ResolvePartyRoleIdAsync(5, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
