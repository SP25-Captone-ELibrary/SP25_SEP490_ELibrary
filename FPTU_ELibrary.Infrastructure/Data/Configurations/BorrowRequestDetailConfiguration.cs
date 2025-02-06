using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class BorrowRequestDetailConfiguration : IEntityTypeConfiguration<BorrowRequestDetail>
{
    public void Configure(EntityTypeBuilder<BorrowRequestDetail> builder)
    {
        #region Added at: 04/02/2025
        builder.HasKey(e => e.BorrowRequestDetailId).HasName("PK_BorrowRequestDetail_BorrowRequestDetailId");

        builder.ToTable("Borrow_Request_Detail");

        builder.Property(e => e.BorrowRequestDetailId).HasColumnName("borrow_request_detail_id");
        builder.Property(e => e.BorrowRequestId).HasColumnName("borrow_request_id");
        builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
        
        builder.HasOne(d => d.BorrowRequest).WithMany(p => p.BorrowRequestDetails)
            .HasForeignKey(d => d.BorrowRequestId)
            .OnDelete(DeleteBehavior.Cascade) // Cascade
            .HasConstraintName("FK_BorrowRequestDetail_BorrowRequestId");
        
        builder.HasOne(d => d.LibraryItem).WithMany(p => p.BorrowRequestDetails)
            .HasForeignKey(d => d.LibraryItemId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRequestDetail_ItemId");
        #endregion

        #region Update at: 05/02/2025
        // builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
        // builder.HasOne(d => d.LibraryItemInstance).WithMany(p => p.BorrowRequestDetails)
        //     .HasForeignKey(d => d.LibraryItemInstanceId)
        //     .OnDelete(DeleteBehavior.ClientSetNull)
        //     .HasConstraintName("FK_BorrowRequestDetail_ItemInstanceId");
        #endregion
    }
}