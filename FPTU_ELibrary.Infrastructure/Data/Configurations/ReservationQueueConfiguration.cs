using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class ReservationQueueConfiguration : IEntityTypeConfiguration<ReservationQueue>
    {
        public void Configure(EntityTypeBuilder<ReservationQueue> builder)
        {
            builder.HasKey(e => e.QueueId).HasName("PK_ReservationQueue_QueueId");

            builder.ToTable("Reservation_Queue");

            builder.Property(e => e.QueueId).HasColumnName("queue_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.DepositExpirationDate)
                .HasColumnType("datetime")
                .HasColumnName("deposit_expiration_date");
            builder.Property(e => e.DepositFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("deposit_fee");
            builder.Property(e => e.DepositPaid)
                .HasDefaultValue(false)
                .HasColumnName("deposit_paid");
            builder.Property(e => e.ExpectedAvailableDate)
                .HasColumnType("datetime")
                .HasColumnName("expected_available_date");
            builder.Property(e => e.QueueStatus)
                .HasMaxLength(50)
                .HasColumnName("queue_status");
            builder.Property(e => e.ReservationDate)
                .HasColumnType("datetime")
                .HasColumnName("reservation_date");
            builder.Property(e => e.ReservedBy).HasColumnName("reserved_by");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.ReservationQueues)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_BookEditionId");

            builder.HasOne(d => d.ReservedByNavigation).WithMany(p => p.ReservationQueues)
                .HasForeignKey(d => d.ReservedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationQueue_ReservedBy");
        }
    }
}
