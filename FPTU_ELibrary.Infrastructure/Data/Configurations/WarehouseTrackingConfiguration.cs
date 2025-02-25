using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class WarehouseTrackingConfiguration : IEntityTypeConfiguration<WarehouseTracking>
{
    public void Configure(EntityTypeBuilder<WarehouseTracking> builder)
    {
       #region Added at 16/01/2025 by Le Xuan Phuoc
       builder.HasKey(e => e.TrackingId).HasName("PK_WarehouseTracking_TrackingId");
       
       builder.ToTable("Warehouse_Tracking");
       
       builder.Property(e => e.TrackingId)
              .HasColumnName("tracking_id")
              .IsRequired();
       builder.Property(e => e.EntryDate)
              .HasColumnType("datetime")
              .HasColumnName("entry_date")
              .IsRequired();
       builder.Property(e => e.TotalItem)
              .HasColumnName("total_item")
              .IsRequired();
       builder.Property(e => e.TotalAmount)
              .HasColumnName("total_amount")
              .HasColumnType("decimal(18, 2)")
              .HasDefaultValue(0.0m);
       builder.Property(e => e.ReceiptNumber)
              .HasColumnName("receipt_number")
              .HasMaxLength(50)
              .IsRequired();
       builder.Property(e => e.TrackingType)
              .HasColumnName("tracking_type")
              .HasMaxLength(30)
              .IsRequired();
       builder.Property(e => e.TransferLocation)
              .HasColumnName("transfer_location")
              .HasMaxLength(255);
       builder.Property(e => e.Description)
              .HasColumnName("description")
              .HasMaxLength(255);
       builder.Property(e => e.Status)
              .HasColumnName("status")
              .HasMaxLength(30)
              .IsRequired();
       builder.Property(e => e.ExpectedReturnDate)
              .HasColumnType("datetime")
              .HasColumnName("expected_return_date");
       builder.Property(e => e.ActualReturnDate)
              .HasColumnType("datetime")
              .HasColumnName("actual_return_date");
       builder.Property(e => e.SupplierId)
              .HasColumnName("supplier_id")
              .IsRequired();
       builder.HasOne(e => e.Supplier).WithMany(p => p.WarehouseTrackings)
              .HasForeignKey(e => e.SupplierId)
              .OnDelete(DeleteBehavior.ClientSetNull)
              .HasConstraintName("FK_WarehouseTracking_SupplierId");
       builder.Property(e => e.CreatedAt)
              .HasColumnType("datetime")
              .HasColumnName("created_at")
              .IsRequired();
       builder.Property(e => e.CreatedBy)
              .HasColumnName("created_by")
              .HasMaxLength(255)
              .IsRequired();
       builder.Property(e => e.UpdatedAt)
              .HasColumnType("datetime")
              .HasColumnName("updated_at");
       builder.Property(e => e.UpdatedBy)
              .HasColumnName("updated_by")
              .HasMaxLength(255);
       #endregion
    }
}