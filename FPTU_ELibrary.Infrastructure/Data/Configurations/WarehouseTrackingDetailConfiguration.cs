using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class WarehouseTrackingDetailConfiguration : 
    IEntityTypeConfiguration<WarehouseTrackingDetail>
{
    public void Configure(EntityTypeBuilder<WarehouseTrackingDetail> builder)
    {
        builder.HasKey(e => e.TrackingDetailId).HasName("PK_WarehouseTrackingDetail_TrackingDetailId");
        
        builder.ToTable("Warehouse_Tracking_Detail");

        builder.Property(e => e.TrackingDetailId)
            .HasColumnName("tracking_detail_id")
            .IsRequired();
        builder.Property(e => e.TrackingId)
            .HasColumnName("tracking_id")
            .IsRequired();
        builder.Property(e => e.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();
        builder.Property(e => e.LibraryItemId)
            .HasColumnName("library_item_id");
        builder.Property(e => e.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(e => e.ItemTotal)
            .HasColumnName("item_total")
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(e => e.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(10, 2)")
            .IsRequired()
            .HasDefaultValue(0.0m);
        builder.Property(e => e.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("decimal(18, 2)")
            .IsRequired()
            .HasDefaultValue(0.0m);
        builder.Property(e => e.Reason)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("reason");
        
        builder.HasOne(e => e.LibraryItem)
            .WithMany(p => p.WarehouseTrackingDetails)
            .HasForeignKey(e => e.LibraryItemId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_WarehouseTrackingDetail_LibraryItemId");

        builder.HasOne(e => e.WarehouseTracking)
            .WithMany(p => p.WarehouseTrackingDetails)
            .HasForeignKey(e => e.TrackingId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_WarehouseTrackingDetail_TrackingId");

        builder.HasOne(e => e.Category)
            .WithMany(p => p.WarehouseTrackingDetails)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_WarehouseTrackingDetail_CategoryId");

        #region Updated at 31/01/2025 by Le Xuan Phuoc
        builder.Property(e => e.Isbn)
            .HasMaxLength(13)
            .HasColumnName("isbn");
        #endregion

        #region Update at 13/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.ConditionId).HasColumnName("condition_id");
        builder.HasOne(e => e.Condition).WithMany(p => p.WarehouseTrackingDetails)
            .HasForeignKey(e => e.ConditionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_WarehouseTrackingDetail_ConditionId");
        #endregion

        #region Update at 18/02/2025 by Le Xuan Phuoc
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