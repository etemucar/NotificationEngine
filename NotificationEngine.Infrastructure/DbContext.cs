using Microsoft.EntityFrameworkCore;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Base;
using System.Linq.Expressions;
using NotificationEngine.Core.Converter;
using NotificationEngine.Core.TMFCommon;
using SchedulerEngine.Data.Seeding;


namespace NotificationEngine.Infrastructure;

public class NotificationEngineDbContext : DbContext
{ 
    public NotificationEngineDbContext(DbContextOptions<NotificationEngineDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> ApplicationUser { get; set; }
    public DbSet<ContactMedium> ContactMedium { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Individual> Individual { get; set; }
    public DbSet<Language> Language { get; set; }
    public DbSet<LocalizableFields> LocalizableFields { get; set; }
    public DbSet<Localization> Localization { get; set; }
    public DbSet<Organization> Organization { get; set; }
    public DbSet<OrganizationLanguageRel> OrganizationLanguageRel { get; set; }
    public DbSet<Party> Party { get; set; }
    public DbSet<PartyRole> PartyRole { get; set; }
    public DbSet<PartyRoleAccount> PartyRoleAccount { get; set; }
    public DbSet<PartyRoleType> PartyRoleType { get; set; }
    public DbSet<RelatedParty> RelatedParty { get; set; }
    public DbSet<RelatedPartyType> RelatedPartyType { get; set; }

    public DbSet<MessageTemplate> MessageTemplate { get; set; }
    public DbSet<OutboxMessage> OutboxMessage { get; set; }
    public DbSet<DeliveryLog> DeliveryLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DataSeed.Seed(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationEngineDbContext).Assembly);

        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {

            if (entityType.GetSchema() is null)
            {
                entityType.SetSchema(EntityConfigurationBase.Schema?.ToSnakeCase());
            }
            else
            {
                var currentSchema = entityType.GetSchema();
                if (!string.IsNullOrEmpty(currentSchema))
                {
                    entityType.SetSchema(currentSchema.ToSnakeCase());
                }
            }
			
            var tableName = entityType.GetTableName();

            // Tablo adını snake_case yap
            if (!string.IsNullOrEmpty(tableName))
            {
                entityType.SetTableName(tableName.ToSnakeCase());
                tableName = entityType.GetTableName(); // güncellendi, tekrar al
            }

            // Id kolonunu snake_case tablo adı ile isimlendur
            var idProperty = entityType.FindProperty("Id");
            if (idProperty != null && !string.IsNullOrEmpty(tableName))
            {
                idProperty.SetColumnName($"{tableName}_id");
            }

            // Diğer tüm kolonları snake_case yap
            foreach (var property in entityType.GetProperties())
            {
                if (property == idProperty) continue; // Id zaten ayarlandı
                property.SetColumnName(property.GetColumnName().ToSnakeCase());
            }

            // IsDeleted query filter
            if (entityType.FindProperty("IsDeleted") != null)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod("Property")!
                                                .MakeGenericMethod(typeof(int));
                var isDeletedProperty = Expression.Call(null, propertyMethodInfo, parameter,
                                                        Expression.Constant("IsDeleted"));
                var compareExpression = Expression.NotEqual(isDeletedProperty, Expression.Constant(1));
                var lambda = Expression.Lambda(compareExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }    
    }
}