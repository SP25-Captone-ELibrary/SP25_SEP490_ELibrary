using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class WarehouseTrackingInventoryConfiguration : IEntityTypeConfiguration<WarehouseTrackingInventory>
{
    public void Configure(EntityTypeBuilder<WarehouseTrackingInventory> builder)
    {
        #region Added At 22/02/2025
        builder.HasKey(e => e.TrackingId).HasName("PK_WarehouseTrackingInventory_TrackingId");

        builder.ToTable("Warehouse_Tracking_Inventory");
        
        builder.Property(e => e.TrackingId)
            .ValueGeneratedNever()
            .HasColumnName("tracking_id");
        builder.Property(e => e.TotalItem).HasColumnName("total_item");
        builder.Property(e => e.TotalInstanceItem).HasColumnName("total_instance_item");
        builder.Property(e => e.TotalCatalogedItem).HasColumnName("total_cataloged_item");
        builder.Property(e => e.TotalCatalogedInstanceItem).HasColumnName("total_cataloged_instance_item");
        
        builder.HasOne(d => d.WarehouseTracking).WithOne(p => p.WarehouseTrackingInventory)
            .HasForeignKey<WarehouseTrackingInventory>(d => d.TrackingId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_WarehouseTrackingInventory_TrackingId");
        #endregion
    }
}