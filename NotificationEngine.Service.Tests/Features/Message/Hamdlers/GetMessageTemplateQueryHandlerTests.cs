using FluentAssertions;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class GetMessageTemplateQueryHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly GetMessageTemplateQueryHandler _handler;

    public GetMessageTemplateQueryHandlerTests()
    {
        _handler = new GetMessageTemplateQueryHandler(_repo.Object, TestMapper.Create());
    }

    [Fact]
    public async Task Handle_OwnTemplate_ReturnsIt()
    {
        _repo.Add(1, organizationId: 7, code: "OTP", body: "Kodunuz: {{code}}");

        var response = await _handler.Handle(new GetMessageTemplateQuery(1, 7), TestContext.Current.CancellationToken);

        response.Id.Should().Be(1);
        response.OrganizationId.Should().Be(7);
        response.Code.Should().Be("OTP");
        response.Body.Should().Be("Kodunuz: {{code}}");
    }

    [Fact]
    public async Task Handle_SharedTemplate_IsReadableByAnyOrganization()
    {
        _repo.Add(1, organizationId: null);

        var response = await _handler.Handle(new GetMessageTemplateQuery(1, 7), TestContext.Current.CancellationToken);

        response.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PassiveTemplate_IsStillReadable()
    {
        _repo.Add(1, organizationId: 7, status: GeneralStatus.Passive);

        var response = await _handler.Handle(new GetMessageTemplateQuery(1, 7), TestContext.Current.CancellationToken);

        response.Status.Should().Be(GeneralStatus.Passive);
    }

    [Fact]
    public async Task Handle_OtherOrganizationsTemplate_ThrowsNotFound()
    {
        _repo.Add(1, organizationId: 99);

        var act = () => _handler.Handle(new GetMessageTemplateQuery(1, 7), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrganizationTemplateBySiteAdmin_ThrowsNotFound()
    {
        _repo.Add(1, organizationId: 7);   // SiteAdmin (OrganizationId boş) yalnızca ortak şablonları görür

        var act = () => _handler.Handle(new GetMessageTemplateQuery(1, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_MissingTemplate_ThrowsNotFound()
    {
        var act = () => _handler.Handle(new GetMessageTemplateQuery(42, 7), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*42*");
    }
}