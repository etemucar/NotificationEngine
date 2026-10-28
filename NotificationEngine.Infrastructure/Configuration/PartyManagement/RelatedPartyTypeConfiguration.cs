using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure;

public class RelatedPartyTypeConfiguration : IEntityTypeConfiguration<RelatedPartyType>
{
    public void Configure(EntityTypeBuilder<RelatedPartyType> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.RelatedParties)
               .WithOne(x => x.RelatedPartyType)
               .HasForeignKey(x => x.RelatedPartyTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}