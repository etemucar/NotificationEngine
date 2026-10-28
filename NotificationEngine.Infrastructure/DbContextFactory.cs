using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NotificationEngine.Infrastructure;
public class NotificationEngineDbContextFactory : IDesignTimeDbContextFactory<NotificationEngineDbContext>
{
    public NotificationEngineDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string not found.");

        var optionsBuilder = new DbContextOptionsBuilder<NotificationEngineDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NotificationEngineDbContext(optionsBuilder.Options);
    }
}
