using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationEngine.Service.Mapper;

namespace NotificationEngine.Service.Tests.Mapper;

public class MappingProfileTests
{
    private static MapperConfiguration BuildConfig() =>
        new(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        
    [Fact]
    public void Configuration_ShouldBeValid()
    {
        BuildConfig().AssertConfigurationIsValid();
    }
}
