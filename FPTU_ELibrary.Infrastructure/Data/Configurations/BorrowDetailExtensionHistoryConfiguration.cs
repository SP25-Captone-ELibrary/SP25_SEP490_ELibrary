using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class BorrowDetailExtensionHistoryConfiguration : IEntityTypeConfiguration<BorrowDetailExtensionHistory>
{
    public void Configure(EntityTypeBuilder<BorrowDetailExtensionHistory> builder)
    {
        #region Added at 03/10/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.BorrowDetailExtensionHistoryId)
            .HasName("PK_BorrowDetailExtensionHistory_BorrowDetailExtensionHistoryId");

        builder.ToTable("Borrow_Detail_Extension_History");
        
        builder.Property(e => e.BorrowDetailExtensionHistoryId).HasColumnName("borrow_detail_extension_history_id");
        builder.Property(e => e.ExtensionDate)
            .HasColumnType("datetime")
            .HasColumnName("extension_date");
        builder.Property(e => e.NewExpiryDate)
            .HasColumnType("datetime")
            .HasColumnName("new_expiry_date");
        builder.Property(e => e.ExtensionNumber).HasColumnName("extension_number");
        
        builder.HasOne(e => e.BorrowRecordDetail).WithMany(p => p.BorrowDetailExtensionHistories)
            .HasForeignKey(e => e.BorrowRecordDetailId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowDetailExtensionHistory_BorrowRecordDetailId");
        #endregion
    }
}