using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class DigitalBorrowExtensionHistoryConfiguration : IEntityTypeConfiguration<DigitalBorrowExtensionHistory>
{
    public void Configure(EntityTypeBuilder<DigitalBorrowExtensionHistory> builder)
    {
        #region Added at 03/10/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.DigitalExtensionHistoryId).HasName("PK_DigitalBorrowExtensionHistory_DigitalExtensionHistoryId");

        builder.ToTable("Digital_Borrow_Extension_History");
        
        builder.Property(e => e.DigitalExtensionHistoryId).HasColumnName("digital_extension_history_id");
        builder.Property(e => e.ExtensionDate)
            .HasColumnType("datetime")
            .HasColumnName("extension_date");
        builder.Property(e => e.NewExpiryDate)
            .HasColumnType("datetime")
            .HasColumnName("new_expiry_date");
        builder.Property(e => e.ExtensionFee)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("extension_fee");
        builder.Property(e => e.ExtensionNumber).HasColumnName("extension_number");

        builder.HasOne(e => e.DigitalBorrow).WithMany(p => p.DigitalBorrowExtensionHistories)
            .HasForeignKey(e => e.DigitalBorrowId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_DigitalBorrowExtensionHistory_DigitalBorrowId");
        #endregion
    }
}