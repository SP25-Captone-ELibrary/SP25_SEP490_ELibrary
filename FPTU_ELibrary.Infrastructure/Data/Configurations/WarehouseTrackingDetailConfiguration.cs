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

        #region Update at 19/02/2025 by Le Xuan Phuoc
        // builder.Property(e => e.Reason)
        //     .HasConversion<string>()
        //     .HasColumnType("nvarchar(50)")
        //     .HasColumnName("reason");
        
        // builder.Property(e => e.ReasonId)
        //     .HasColumnName("reason_id")
        //     .IsRequired(false);
        // builder.HasOne(e => e.Reason).WithMany(p => p.WarehouseTrackingDetails)
        //     .HasForeignKey(e => e.ReasonId)
        //     .OnDelete(DeleteBehavior.ClientSetNull)
        //     .HasConstraintName("FK_WarehouseTrackingDetail_ReasonId");
        #endregion

        #region Update at 20/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.BarcodeRangeFrom)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("barcode_range_from");
        builder.Property(e => e.BarcodeRangeTo)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("barcode_range_to");
        #endregion

        #region Update at 21/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.StockTransactionType)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("stock_transaction_type");
        #endregion

        #region Update at 24/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.HasGlueBarcode)
            .HasDefaultValue(false)
            .HasColumnName("has_glue_barcode");
        #endregion

        #region Updated at 07/04/2025 by Le Xuan Phuoc
        builder.Property(e => e.SupplementRequestReason)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("supplement_request_reason");
        builder.Property(e => e.BorrowSuccessCount).HasColumnName("borrow_success_count");
        builder.Property(e => e.ReserveCount).HasColumnName("reserve_count");
        builder.Property(e => e.BorrowFailedCount).HasColumnName("borrow_failed_count");
        builder.Property(e => e.AvailableUnits).HasColumnName("available_units");
        builder.Property(e => e.NeedUnits).HasColumnName("need_units");
        builder.Property(e => e.AverageNeedSatisfactionRate).HasColumnName("average_need_satisfaction_rate");
        #endregion

        #region Updated at 08/05/2025 by Le Xuan Phuoc
        builder.Property(e => e.BorrowRequestCount).HasColumnName("borrow_request_count");
        builder.Property(e => e.TotalSatisfactionUnits).HasColumnName("total_satisfaction_units");
        builder.Property(e => e.BorrowExtensionRate).HasColumnName("borrow_extension_rate");
        // builder.Property(e => e.BorrowFailedRate).HasColumnName("borrow_failed_rate");
        #endregion
    }
}