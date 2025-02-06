using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        #region Added at 16/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.TransactionId).HasName("PK_Transaction_TransactionId");

        builder.ToTable("Transaction");

        builder.Property(e => e.TransactionId).HasColumnName("transaction_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.FineId).HasColumnName("fine_id");
        builder.Property(e => e.InvoiceId).HasColumnName("invoice_id");
        builder.Property(e => e.DigitalBorrowId).HasColumnName("digital_borrow_id");
        builder.Property(e => e.LibraryCardPackageId).HasColumnName("library_card_package_id");
        builder.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
        builder.Property(e => e.TransactionCode)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("transaction_code");
        builder.Property(e => e.Amount)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("amount");
        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("description");
        builder.Property(e => e.TransactionStatus)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("transaction_status");
        builder.Property(e => e.TransactionType)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("transaction_type");
        builder.Property(e => e.TransactionDate)
            .HasColumnType("datetime")
            .HasColumnName("transaction_date");
        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime")
            .HasColumnName("created_at");
        builder.Property(e => e.CancelledAt)
            .HasColumnType("datetime")
            .HasColumnName("canceled_at");
        builder.Property(e => e.CancellationReason)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("cancellation_reason");
        
        builder.HasOne(e => e.User).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_UserId");
        
        builder.HasOne(e => e.Fine).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.FineId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_FineId");
        
        builder.HasOne(e => e.Invoice).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_InvoiceId");
        
        builder.HasOne(e => e.DigitalBorrow).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.DigitalBorrowId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_DigitalBorrowId");
        
        builder.HasOne(e => e.LibraryCardPackage).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.LibraryCardPackageId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_LibraryCardPackageId");
        
        builder.HasOne(e => e.PaymentMethod).WithMany(p => p.Transactions)
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Transaction_PaymentMethodId");
        #endregion
    }
}