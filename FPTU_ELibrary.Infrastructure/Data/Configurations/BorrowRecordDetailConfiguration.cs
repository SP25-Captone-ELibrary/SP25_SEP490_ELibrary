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

        #region Update at: 12/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.ConditionCheckDate)
            .HasColumnType("datetime")
            .HasColumnName("condition_check_date");
        builder.Property(e => e.ReturnConditionId).HasColumnName("return_condition_id");
        #endregion

        #region Update at: 13/02/2025 by Le Xuan Phuoc
        // builder.Property(e => e.BorrowCondition)
        //     .HasMaxLength(50)
        //     .HasColumnName("borrow_condition");
            
        builder.Property(e => e.ConditionId).HasColumnName("condition_id");
        builder.HasOne(d => d.Condition).WithMany(p => p.BorrowRecordDetails)
            .HasForeignKey(d => d.ConditionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRecordDetail_ConditionId");
        #endregion

        #region Updated at: 03/10/2025 by Le Xuan Phuoc
        builder.Property(e => e.IsReminderSent)
            .HasDefaultValue(false)
            .HasColumnName("is_reminder_sent");
        builder.Property(e => e.ReturnDate)
            .HasColumnType("datetime")
            .HasColumnName("return_date");
        builder.Property(e => e.DueDate)
            .HasColumnType("datetime")
            .HasColumnName("due_date");
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("status");
        builder.Property(e => e.TotalExtension)
            .HasDefaultValue(0)
            .HasColumnName("total_extension");
        #endregion
    }
}