using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(x => x.TemplateCode).HasMaxLength(100);
        builder.Property(x => x.Recipient).IsRequired().HasMaxLength(320);
        builder.Property(x => x.Subject).HasMaxLength(500);
        // Body: text; OTP mesajlarında null kalır
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        builder.Property(x => x.LastError).HasMaxLength(2000);

        builder.HasOne(x => x.Organization)
               .WithMany()
               .HasForeignKey(x => x.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);

        // Tekrarlı isteğe karşı: aynı kuruluş + aynı anahtar → tek mesaj
        builder.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();

        // Worker'ın claim sorgusu için kısmi index — sadece aktif kuyruk satırları.
        // UYARI: HasFilter ham SQL'dir, otomatik snake_case dönüşümü İÇİNE işlemez;
        // kolon adı elle snake_case yazıldı. 1 = Pending, 2 = Processing (MessageStatus enum değerleri).
        builder.HasIndex(x => new { x.DeliveryStatus, x.NextAttemptAt })
               .HasFilter("delivery_status IN (1, 2)");
    }
}