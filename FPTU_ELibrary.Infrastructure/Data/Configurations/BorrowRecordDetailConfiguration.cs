using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class BorrowRecordDetailConfiguration : IEntityTypeConfiguration<BorrowRecordDetail>
{
    public void Configure(EntityTypeBuilder<BorrowRecordDetail> builder)
    {
        #region Added at: 04/02/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.BorrowRecordDetailId).HasName("PK_BorrowRecordDetail_BorrowRecordDetailId");

        builder.ToTable("Borrow_Record_Detail");
        
        builder.Property(e => e.BorrowRecordDetailId).HasColumnName("borrow_record_detail_id");
        builder.Property(e => e.BorrowRecordId).HasColumnName("borrow_record_id");
        builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
        
        builder.HasOne(d => d.BorrowRecord).WithMany(p => p.BorrowRecordDetails)
            .HasForeignKey(d => d.BorrowRecordId)
            .OnDelete(DeleteBehavior.Cascade) // Cascade
            .HasConstraintName("FK_BorrowRecordDetail_BorrowRecordId");
        
        builder.HasOne(d => d.LibraryItemInstance).WithMany(p => p.BorrowRecordDetails)
            .HasForeignKey(d => d.LibraryItemInstanceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRecordDetail_ItemInstanceId");
        #endregion

        #region Update at: 06/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.ImagePublicIds)
            .HasColumnType("nvarchar(200)")
            .IsRequired(false)
            .HasColumnName("image_public_ids");
        #endregion
    }
}