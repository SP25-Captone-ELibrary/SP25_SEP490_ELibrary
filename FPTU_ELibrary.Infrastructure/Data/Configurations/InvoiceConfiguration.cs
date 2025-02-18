using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        #region Added at 16/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.InvoiceId).HasName("PK_Invoice_InvoiceId");
        
        builder.ToTable("Invoice");
        
        builder.Property(e => e.InvoiceId).HasColumnName("invoice_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.TotalAmount)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("total_amount");
        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime")
            .HasColumnName("created_at");
        builder.Property(e => e.PaidAt)
            .HasColumnType("datetime")
            .HasColumnName("paid_at");
        
        builder.HasOne(e => e.User).WithMany(p => p.Invoices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Invoice_UserId");
        #endregion

        #region Update at 17/02/2025
        // builder.Property(e => e.Status)
        //     .HasConversion<string>()
        //     .HasColumnType("nvarchar(50)")
        //     .HasColumnName("status");
        #endregion
    }
}