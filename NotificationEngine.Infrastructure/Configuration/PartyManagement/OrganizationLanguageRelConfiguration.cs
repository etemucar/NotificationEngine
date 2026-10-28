using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure.Configurations;

public class OrganizationLanguageRelConfiguration : IEntityTypeConfiguration<OrganizationLanguageRel>
{
    public void Configure(EntityTypeBuilder<OrganizationLanguageRel> builder)
    {
        builder.HasOne(x => x.Organization)
            .WithMany(x => x.OrganizationLanguageRels)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany(x => x.OrganizationLanguageRels)
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.LanguageId })
            .IsUnique();
    }
}
