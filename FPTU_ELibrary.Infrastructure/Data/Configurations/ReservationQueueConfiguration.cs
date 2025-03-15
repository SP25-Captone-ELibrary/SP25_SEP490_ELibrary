using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class ReservationQueueConfiguration : IEntityTypeConfiguration<ReservationQueue>
    {
        public void Configure(EntityTypeBuilder<ReservationQueue> builder)
        {
            builder.HasKey(e => e.QueueId).HasName("PK_ReservationQueue_QueueId");

            builder.ToTable("Reservation_Queue");

            builder.Property(e => e.QueueId).HasColumnName("queue_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
            builder.Property(e => e.QueueStatus)
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)")
                .HasColumnName("queue_status");
            builder.Property(e => e.ReservationDate)
                .HasColumnType("datetime")
                .HasColumnName("reservation_date");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.ReservationQueues)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_ItemId");

            #region Update at: 04/02/2025 by Le Xuan Phuoc
            // builder.Property(e => e.DepositExpirationDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("deposit_expiration_date");
            // builder.Property(e => e.DepositFee)
            //     .HasColumnType("decimal(10, 2)")
            //     .HasColumnName("deposit_fee");
            // builder.Property(e => e.DepositPaid)
            //     .HasDefaultValue(false)
            //     .HasColumnName("deposit_paid");
            
            // builder.Property(e => e.ReservedBy).HasColumnName("reserved_by");
            // builder.HasOne(d => d.ReservedByNavigation).WithMany(p => p.ReservationQueues)
            //     .HasForeignKey(d => d.ReservedBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_ReservationQueue_ReservedBy");
            
            // builder.Property(e => e.ExpectedAvailableDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("expected_available_date");
            
            builder.Property(e => e.ExpectedAvailableDateMax)
                .HasColumnType("datetime")
                .HasColumnName("expected_available_date_max");
            
            builder.Property(e => e.ExpectedAvailableDateMin)
                .HasColumnType("datetime")
                .HasColumnName("expected_available_date_min");
            
            builder.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiry_date");
            
            builder.Property(e => e.IsNotified).HasColumnName("is_notified");
            
            builder.Property(e => e.CancelledBy)
                .HasColumnType("nvarchar(100)")
                .HasColumnName("cancelled_by");

            builder.Property(e => e.CancellationReason)
                .HasColumnType("nvarchar(500)")
                .HasColumnName("cancellation_reason");
            
            builder.Property(e => e.LibraryCardId).HasColumnName("library_card_id");
            builder.HasOne(d => d.LibraryCard).WithMany(p => p.ReservationQueues)
                .HasForeignKey(d => d.LibraryCardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_LibraryCardId");
            
            builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
            builder.HasOne(d => d.LibraryItemInstance).WithMany(p => p.ReservationQueues)
                .HasForeignKey(d => d.LibraryItemInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_LibraryItemInstanceId");
            #endregion

            #region Updated at: 14/03/2025 by Le Xuan Phuoc
            builder.Property(e => e.IsReservedAfterRequestFailed)
                .HasDefaultValue(false)
                .HasColumnName("is_reserved_after_request_failed");
            
            builder.Property(e => e.BorrowRequestId).HasColumnName("borrow_request_id");
            builder.HasOne(e => e.BorrowRequest).WithMany(p => p.ReservationQueues)
                .HasForeignKey(e => e.BorrowRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_BorrowRequestId");
            #endregion
        }
    }
}
