using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure;

public class DeliveryLogConfiguration : IEntityTypeConfiguration<DeliveryLog>
{
    public void Configure(EntityTypeBuilder<DeliveryLog> builder)
    {
        builder.Property(x => x.Detail).HasMaxLength(2000);

        builder.HasOne(x => x.Message)
               .WithMany(m => m.DeliveryLogs)
               .HasForeignKey(x => x.MessageId)
               .OnDelete(DeleteBehavior.Cascade); // retention işi outbox satırını silince loglar da gider

        builder.HasIndex(x => x.MessageId);
    }
}