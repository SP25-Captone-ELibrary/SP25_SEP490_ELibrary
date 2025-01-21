using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryCardConfiguration : IEntityTypeConfiguration<LibraryCard>
{
    public void Configure(EntityTypeBuilder<LibraryCard> builder)
    {
        #region Added at 13/01/2025 by Le Xuan Phuoc

        builder.HasKey(e => e.LibraryCardId).HasName("PK_LibraryCard_LibraryCardId");
        
        builder.ToTable("Library_Card");

        builder.Property(e => e.LibraryCardId)
            .HasDefaultValueSql("(newsequentialid())")
            .HasColumnName("library_card_id");
        builder.Property(e => e.FullName)
            .HasColumnType("nvarchar(200)")
            .HasColumnName("full_name");
        builder.Property(e => e.Avatar)
            .HasMaxLength(2048)
            .IsUnicode(false)
            .HasColumnName("avatar");
        builder.Property(e => e.Barcode)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("barcode");
        builder.Property(e => e.IssuanceMethod)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("issuance_method");
        builder.Property(e => e.RequestStatus)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("request_status");
        builder.Property(e => e.IssueDate)
            .HasColumnType("datetime")
            .HasColumnName("issue_date");
        builder.Property(e => e.ExpiryDate)
            .HasColumnType("datetime")
            .HasColumnName("expiry_date");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.IsExtended).HasColumnName("is_extended");
        builder.Property(e => e.ExtensionCount).HasColumnName("extension_count");
        #endregion
    }
}