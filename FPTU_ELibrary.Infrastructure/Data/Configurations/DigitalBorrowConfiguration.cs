using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class DigitalBorrowConfiguration : IEntityTypeConfiguration<DigitalBorrow>
{
    public void Configure(EntityTypeBuilder<DigitalBorrow> builder)
    {
        #region Added at 16/01/2025 by Le Xuan Phuoc

        builder.HasKey(e => e.DigitalBorrowId).HasName("PK_DigitalBorrow_DigitalBorrowId");

        builder.ToTable("Digital_Borrow");

        builder.Property(e => e.DigitalBorrowId).HasColumnName("digital_borrow_id");
        builder.Property(e => e.ResourceId).HasColumnName("resource_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.RegisterDate)
            .HasColumnType("datetime")
            .HasColumnName("register_date");
        builder.Property(e => e.ExpiryDate)
            .HasColumnType("datetime")
            .HasColumnName("expiry_date");
        builder.Property(e => e.IsExtended).HasColumnName("is_extended");
        builder.Property(e => e.S3WatermarkedName)
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("s3_watermarked_name");
        builder.Property(e => e.ExtensionCount).HasColumnName("extension_count");
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("status");

        builder.HasOne(e => e.LibraryResource).WithMany(p => p.DigitalBorrows)
            .HasForeignKey(e => e.ResourceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_DigitalBorrow_ResourceId");

        builder.HasOne(e => e.User).WithMany(p => p.DigitalBorrows)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_DigitalBorrow_UserId");

        #endregion
    }
}