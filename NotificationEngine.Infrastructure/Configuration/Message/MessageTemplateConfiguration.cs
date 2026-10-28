using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure;

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Culture).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.Body).IsRequired(); // text, uzunluk sınırı yok

        builder.HasOne(x => x.Organization)
               .WithMany()
               .HasForeignKey(x => x.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);

        // OrganizationId null = ortak şablon. Postgres'te NULL'lar varsayılan olarak "farklı" sayılır,
        // bu yüzden aynı ortak şablon iki kez eklenebilirdi — NULLS NOT DISTINCT bunu engeller (PG 15+).
        builder.HasIndex(x => new { x.OrganizationId, x.Code, x.Channel, x.Culture })
               .IsUnique()
               .AreNullsDistinct(false);
    }
}