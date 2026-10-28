using FluentAssertions;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class GetMessageTemplateListQueryHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly GetMessageTemplateListQueryHandler _handler;

    public GetMessageTemplateListQueryHandlerTests()
    {
        _handler = new GetMessageTemplateListQueryHandler(_repo.Object, TestMapper.Create());
    }

    private static GetMessageTemplateListQuery Query(
        int? organizationId = 7, bool includeShared = false, int offset = 0, int limit = 20,
        string? code = null, Channel? channel = null, GeneralStatus? status = null) =>
        new(organizationId, includeShared, offset, limit, code, channel, status);

    private async Task<NotificationEngine.Service.Dtos.Responses.MessageTemplateListResponse> Run(
        GetMessageTemplateListQuery query) =>
        await _handler.Handle(query, TestContext.Current.CancellationToken);

    private void Seed()
    {
        _repo.Add(1, organizationId: 7);      // kuruluşun kendi şablonu
        _repo.Add(2, organizationId: null);   // ortak
        _repo.Add(3, organizationId: 99);     // başka kuruluşun
    }

    [Fact]
    public async Task Handle_SharedNotIncluded_ReturnsOnlyOwnTemplates()
    {
        Seed();

        var result = await Run(Query(includeShared: false));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1 });
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SharedIncluded_ReturnsOwnAndSharedButNotOthers()
    {
        Seed();

        var result = await Run(Query(includeShared: true));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SiteAdmin_ReturnsOnlySharedTemplates()
    {
        Seed();

        var result = await Run(Query(organizationId: null, includeShared: false));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2 });
    }

    [Fact]
    public async Task Handle_FilterByCode_ReturnsMatchingOnly()
    {
        _repo.Add(1, 7, code: "OTP");
        _repo.Add(2, 7, code: "PASSWORD_RESET");

        var result = await Run(Query(code: "OTP"));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1 });
    }

    [Fact]
    public async Task Handle_FilterByChannel_ReturnsMatchingOnly()
    {
        _repo.Add(1, 7, channel: Channel.Sms);
        _repo.Add(2, 7, channel: Channel.Email, subject: "Konu");

        var result = await Run(Query(channel: Channel.Email));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2 });
    }

    [Fact]
    public async Task Handle_FilterByStatus_ReturnsMatchingOnly()
    {
        _repo.Add(1, 7, code: "A", status: GeneralStatus.Active);
        _repo.Add(2, 7, code: "B", status: GeneralStatus.Passive);

        var result = await Run(Query(status: GeneralStatus.Passive));

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2 });
    }

    [Fact]
    public async Task Handle_NoStatusFilter_IncludesPassiveTemplates()
    {
        _repo.Add(1, 7, code: "A", status: GeneralStatus.Active);
        _repo.Add(2, 7, code: "B", status: GeneralStatus.Passive);

        var result = await Run(Query());

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Paging_ReturnsSliceAndTotalOfAllMatches()
    {
        for (var i = 1; i <= 5; i++)
            _repo.Add(i, 7, code: $"CODE_{i}");

        var result = await Run(Query(offset: 2, limit: 2));

        result.Items.Select(i => i.Code).Should().Equal("CODE_3", "CODE_4");
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_OrdersByCodeThenChannelThenCulture()
    {
        _repo.Add(1, 7, code: "B", channel: Channel.Sms, culture: "tr-TR");
        _repo.Add(2, 7, code: "A", channel: Channel.Sms, culture: "tr-TR");
        _repo.Add(3, 7, code: "A", channel: Channel.Email, culture: "tr-TR", subject: "Konu");
        _repo.Add(4, 7, code: "A", channel: Channel.Sms, culture: "en-US");

        var result = await Run(Query());

        result.Items.Select(i => i.Id).Should().Equal(3, 4, 2, 1);
    }
}