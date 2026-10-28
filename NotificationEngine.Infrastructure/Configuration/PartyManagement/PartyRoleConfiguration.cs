
using NotificationEngine.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationEngine.Infrastructure.Configuration;

public class PartyRoleConfiguration : IEntityTypeConfiguration<PartyRole>
{
    public void Configure(EntityTypeBuilder<PartyRole> builder)
    {
        builder.HasOne(x => x.PartyRoleType)
            .WithMany(x => x.PartyRoles)
            .HasForeignKey(x => x.PartyRoleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

