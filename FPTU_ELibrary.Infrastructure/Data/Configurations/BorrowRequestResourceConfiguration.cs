using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class BorrowRequestResourceConfiguration : IEntityTypeConfiguration<BorrowRequestResource>
{
    public void Configure(EntityTypeBuilder<BorrowRequestResource> builder)
    {
        #region Added at: 23/03/2025
        builder.HasKey(e => e.BorrowRequestResourceId).HasName("PK_BorrowRequestResource_RequestResourceId");

        builder.ToTable("Borrow_Request_Resource");

        builder.Property(e => e.BorrowRequestResourceId).HasColumnName("borrow_request_resource_id");
        builder.Property(e => e.ResourceTitle)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("resource_title");
        builder.Property(e => e.DefaultBorrowDurationDays)
            .HasColumnName("default_borrow_duration_days");
        builder.Property(e => e.BorrowPrice)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("borrow_price");
        
        builder.Property(e => e.BorrowRequestId).HasColumnName("borrow_request_id");
        builder.HasOne(e => e.BorrowRequest).WithMany(p => p.BorrowRequestResources)
            .HasForeignKey(e => e.BorrowRequestId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRequestResource_RequestId");
        
        builder.Property(e => e.ResourceId).HasColumnName("resource_id");
        builder.HasOne(e => e.LibraryResource).WithMany(p => p.BorrowRequestResources)
            .HasForeignKey(e => e.ResourceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRequestResource_ResourceId");
        
        builder.Property(e => e.TransactionId).HasColumnName("transaction_id");
        builder.HasOne(e => e.Transaction).WithMany(p => p.BorrowRequestResources)
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BorrowRequestResource_TransactionId");
        #endregion
    }
}