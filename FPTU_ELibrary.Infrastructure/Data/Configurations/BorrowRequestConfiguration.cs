using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BorrowRequestConfiguration : IEntityTypeConfiguration<BorrowRequest>
    {
        public void Configure(EntityTypeBuilder<BorrowRequest> builder)
        {
            builder.HasKey(e => e.BorrowRequestId).HasName("PK_BorrowRequest_BorrowRequestId");

            builder.ToTable("Borrow_Request");

            builder.Property(e => e.BorrowRequestId).HasColumnName("borrow_request_id");
            builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
            builder.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            builder.Property(e => e.ExpirationDate)
                .HasColumnType("datetime")
                .HasColumnName("expiration_date");
            builder.Property(e => e.RequestDate)
                .HasColumnType("datetime")
                .HasColumnName("request_date");
            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.BorrowRequests)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BorrowRequest_ItemId");

            builder.HasOne(d => d.LibraryItemInstance).WithMany(p => p.BorrowRequests)
                .HasForeignKey(d => d.LibraryItemInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BorrowRequest_ItemInstanceId");

            //builder.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.BorrowRequests)
            //    .HasForeignKey(d => d.ProcessedBy)
            //    .HasConstraintName("FK_BorrowRequest_ProcessedBy");

            builder.HasOne(d => d.User).WithMany(p => p.BorrowRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BorrowRequest_UserId");

			#region Update at: 11-10-2024 by Le Xuan Phuoc
			// builder.ToTable(b => b.HasCheckConstraint("CK_BorrowRequest_BookOrMaterial",
			//    "(book_edition_id IS NOT NULL AND learning_material_id IS NULL) OR " +
			//    "(book_edition_id IS NULL AND learning_material_id IS NOT NULL)"));
			#endregion

            #region Update at 14/01/2025 by Le Xuan Phuoc
            // builder.Property(e => e.BorrowType)
            //     .HasMaxLength(50)
            //     .HasColumnName("borrow_type");
            // builder.Property(e => e.DepositFee)
            //     .HasColumnType("decimal(10, 2)")
            //     .HasColumnName("deposit_fee");
            // builder.Property(e => e.DepositPaid).HasColumnName("deposit_paid");
            // builder.Property(e => e.LearningMaterialId).HasColumnName("learning_material_id");
            //builder.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            //builder.Property(e => e.ProcessedDate)
            //.HasColumnType("datetime")
            //.HasColumnName("processed_date");
            #endregion
        }
	}
}
