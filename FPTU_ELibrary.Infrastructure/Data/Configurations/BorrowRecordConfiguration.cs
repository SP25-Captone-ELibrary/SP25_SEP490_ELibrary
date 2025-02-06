using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BorrowRecordConfiguration : IEntityTypeConfiguration<BorrowRecord>
    {
        public void Configure(EntityTypeBuilder<BorrowRecord> builder)
        {
            builder.HasKey(e => e.BorrowRecordId).HasName("PK_BorrowRecord_BorrowRecordId");

            builder.ToTable("Borrow_Record");

            builder.Property(e => e.BorrowRecordId).HasColumnName("borrow_record_id");
            builder.Property(e => e.BorrowCondition)
                .HasMaxLength(50)
                .HasColumnName("borrow_condition");
            builder.Property(e => e.BorrowDate)
                .HasColumnType("datetime")
                .HasColumnName("borrow_date");
            builder.Property(e => e.ConditionCheckDate)
                .HasColumnType("datetime")
                .HasColumnName("condition_check_date");
            builder.Property(e => e.DueDate)
                .HasColumnType("datetime")
                .HasColumnName("due_date");
            builder.Property(e => e.ExtensionLimit).HasColumnName("extension_limit");
            builder.Property(e => e.ReturnCondition)
                .HasMaxLength(50)
                .HasColumnName("return_condition");
            builder.Property(e => e.ReturnDate)
                .HasColumnType("datetime")
                .HasColumnName("return_date");

			#region Update at: 11-10-2024 by Le Xuan Phuoc
			builder.Property(e => e.ProcessedDate)
				.HasColumnType("datetime")
				.HasColumnName("processed_date");
            builder.Property(e => e.ProcessedBy).HasColumnName("proceesed_by");
            builder.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.BorrowRecords)
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BorrowRecord_ProcessedBy");
			#endregion

            #region Update at: 15-01-2025 by Le Xuan Phuoc
            // builder.Property(e => e.DepositRefunded).HasColumnName("deposit_refunded");
            // builder.Property(e => e.RefundDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("refund_date");
            // builder.ToTable(b => b.HasCheckConstraint("CK_BorrowRecord_BookOrMaterial",
            //     "(book_edition_copy_id IS NOT NULL AND learning_material_id IS NULL) OR " +
            //     "(book_edition_copy_id IS NULL AND learning_material_id IS NOT NULL)"));
            #endregion

            #region Update at: 04/02/2025 by Le Xuan Phuoc
            // builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
            // builder.HasOne(d => d.LibraryItemInstance).WithMany(p => p.BorrowRecords)
            //     .HasForeignKey(d => d.LibraryItemInstanceId)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BorrowRecord_ItemInstanceId");
            // builder.Property(e => e.DepositFee)
            //     .HasColumnType("decimal(10,2)")
            //     .HasColumnName("deposit_fee");
            
            // builder.Property(e => e.BorrowerId).HasColumnName("borrower_id");
            // builder.HasOne(d => d.Borrower).WithMany(p => p.BorrowRecords)
            //     .HasForeignKey(d => d.BorrowerId)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BorrowRecord_BorrowerId");
            
            // builder.Property(e => e.BorrowType)
            //     .HasConversion<string>()
            //     .HasColumnType("nvarchar(50)")
            //     .HasColumnName("borrow_type");
            
            // builder.Property(e => e.RequestDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("request_date");
            
            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)")
                .HasColumnName("status");
            
            builder.Property(e => e.SelfServiceBorrow).HasColumnName("self_service_borrow");
            
            builder.Property(e => e.LibraryCardId).HasColumnName("library_card_id");
            builder.HasOne(d => d.LibraryCard).WithMany(p => p.BorrowRecords)
                .HasForeignKey(d => d.LibraryCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BorrowRecord_LibraryCardId");
            
            builder.Property(e => e.BorrowRequestId).HasColumnName("borrow_request_id");
            builder.HasOne(br => br.BorrowRequest)
                .WithOne()
                .HasForeignKey<BorrowRecord>(br => br.BorrowRequestId)
                .IsRequired(false) // Allow NULL for in-person borrowing
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BorrowRecord_BorrowRequestId");

            builder.HasIndex(br => br.BorrowRequestId).IsUnique(); // Ensure only one BorrowRecord per BorrowRequest
            #endregion
        }
	}
}
