using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Mapper;

namespace NotificationEngine.Service.Tests.Features.Handlers;

/// <summary>
/// Şablon repository'sinin bellek içi sahtesi. Items = "veritabanı" (sorgular bunu görür);
/// Added = AddAsync ile izlenmeye alınan ama henüz kaydedilmemiş kayıtlar (sorgularda görünmez).
/// </summary>
internal sealed class TemplateRepositoryFake
{
    public List<MessageTemplate> Items { get; } = new();
    public List<MessageTemplate> Added { get; } = new();
    public List<MessageTemplate> Updated { get; } = new();
    public Mock<IRepository<MessageTemplate, int>> Mock { get; } = new();
    public IRepository<MessageTemplate, int> Object => Mock.Object;

    public TemplateRepositoryFake()
    {
        Mock.Setup(x => x.AnyAsync(
                It.IsAny<Expression<Func<MessageTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<MessageTemplate, bool>> p, CancellationToken _) =>
                Task.FromResult(Items.AsQueryable().Any(p)));

        Mock.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((int id, CancellationToken _) =>
                Task.FromResult(Items.FirstOrDefault(t => t.Id == id)));

        Mock.Setup(x => x.AddAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTemplate, CancellationToken>((t, _) => Added.Add(t))
            .Returns(Task.CompletedTask);

        Mock.Setup(x => x.UpdateAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTemplate, CancellationToken>((t, _) => Updated.Add(t))
            .Returns(Task.CompletedTask);

        Mock.Setup(x => x.FindOffsetAsync(
                It.IsAny<Expression<Func<MessageTemplate, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Func<IQueryable<MessageTemplate>, IOrderedQueryable<MessageTemplate>>?>(),
                It.IsAny<Func<IQueryable<MessageTemplate>, IQueryable<MessageTemplate>>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<MessageTemplate, bool>> p, int offset, int limit,
                      Func<IQueryable<MessageTemplate>, IOrderedQueryable<MessageTemplate>>? orderBy,
                      Func<IQueryable<MessageTemplate>, IQueryable<MessageTemplate>>? _, CancellationToken _) =>
            {
                IQueryable<MessageTemplate> query = Items.AsQueryable().Where(p);
                if (orderBy is not null) query = orderBy(query);
                return Task.FromResult<IReadOnlyList<MessageTemplate>>(query.Skip(offset).Take(limit).ToList());
            });

        Mock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<MessageTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<MessageTemplate, bool>> p, CancellationToken _) =>
                Task.FromResult(Items.AsQueryable().Count(p)));
    }

    public MessageTemplate Add(
        int id, int? organizationId, string code = "OTP", Channel channel = Channel.Sms,
        string culture = "tr-TR", GeneralStatus status = GeneralStatus.Active,
        string? subject = null, string body = "eski")
    {
        var template = new MessageTemplate
        {
            Id = id,
            OrganizationId = organizationId,
            Code = code,
            Channel = channel,
            Culture = culture,
            Subject = subject,
            Body = body,
            Status = status
        };
        Items.Add(template);
        return template;
    }
}

internal static class TestMapper
{
    // Gerçek profil: MessageTemplate → MessageTemplateResponse eşlemesi de doğrulanmış olur.
    public static IMapper Create() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
}