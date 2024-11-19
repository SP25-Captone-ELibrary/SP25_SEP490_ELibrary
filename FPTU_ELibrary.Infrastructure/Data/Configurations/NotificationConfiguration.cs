using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(e => e.NotificationId).HasName("PK_Notification_NotificationId");

            builder.ToTable("Notification");

            builder.Property(e => e.NotificationId).HasColumnName("notification_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsPublic).HasColumnName("is_public");
            builder.Property(e => e.Message).HasColumnName("message");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            builder.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_CreatedBy");
        }
    }
}
