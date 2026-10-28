using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace DocDes.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne(x => x.Language)
            .WithMany(x => x.ApplicationUsers)
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);   // dil silinirse kullanıcılar yetim kalmasın

        builder.HasOne(x => x.DigitalIdentity)
            .WithOne(x => x.ApplicationUser)
            .HasForeignKey<ApplicationUser>(x => x.DigitalIdentityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
