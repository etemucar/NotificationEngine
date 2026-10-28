using NotificationEngine.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationEngine.Infrastructure.Configuration;

public class RelatedPartyConfiguration : IEntityTypeConfiguration<RelatedParty>
{
    public void Configure(EntityTypeBuilder<RelatedParty> builder)
    {
        builder.Property(r => r.RelatedPartyId).HasColumnName("related_to_party_id");

        builder.HasOne(r => r.Party)
               .WithMany(p => p.RelatedParties)
               .HasForeignKey(r => r.PartyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Related)
               .WithMany()
               .HasForeignKey(r => r.RelatedPartyId)
               .OnDelete(DeleteBehavior.Restrict);

        // Kullanıcı başına tek açık OrganizationMember kaydı (related_party_type_id = 1).
        builder.HasIndex(r => r.PartyId)
               .IsUnique()
               .HasDatabaseName("ux_related_party_org_member")
               .HasFilter("related_party_type_id = 1 AND valid_to IS NULL AND is_deleted <> 1");               
    }
}
