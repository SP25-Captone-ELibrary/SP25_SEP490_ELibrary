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
            builder.Property(e => e.IsPublic).HasColumnName("is_public");

            #region Update 27/02/2025
            builder.Property(e => e.Message)
                .HasColumnType("nvarchar(4000)")
                .HasColumnName("message");
            builder.Property(e => e.Title)
                .HasColumnType("nvarchar(150)")
                .HasColumnName("title");
            builder.Property(e => e.NotificationType)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)")
                .HasColumnName("notification_type");
            
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.HasOne(e => e.CreatedByNavigation).WithMany(e => e.Notifications)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_CreatedBy");
            #endregion
        }
    }
}
