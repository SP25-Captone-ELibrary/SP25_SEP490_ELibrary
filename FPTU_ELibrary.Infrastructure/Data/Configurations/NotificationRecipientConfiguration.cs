using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
    {
        public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
        {
            builder.HasKey(e => e.NotificationRecipientId).HasName("PK_NotificationRecipient_NotificationRecipientId");

            builder.ToTable("Notification_Recipient");

            builder.Property(e => e.NotificationRecipientId).HasColumnName("notification_recipient_id");
            builder.Property(e => e.IsRead).HasColumnName("is_read");
            builder.Property(e => e.NotificationId).HasColumnName("notification_id");
            builder.Property(e => e.RecipientId).HasColumnName("recipient_id");

            builder.HasOne(d => d.Notification).WithMany(p => p.NotificationRecipients)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_NotificationRecipient_NotificationId");

            builder.HasOne(d => d.Recipient).WithMany(p => p.NotificationRecipients)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationRecipient_UserId");
        }
    }
}
