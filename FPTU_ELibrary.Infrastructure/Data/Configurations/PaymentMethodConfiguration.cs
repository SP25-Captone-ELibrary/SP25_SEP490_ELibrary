using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        #region Added at 16/01/2025
        builder.HasKey(e => e.PaymentMethodId).HasName("PK_PaymentMethod_PaymentMethodId");

        builder.ToTable("Payment_Method");
        
        builder.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
        builder.Property(e => e.MethodName)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("method_name");
        #endregion
    }
}