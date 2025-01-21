using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        #region Added at 16/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.SupplierId).HasName("PK_Supplier_SupplierId");

        builder.ToTable("Supplier");
        
        builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
        builder.Property(e => e.SupplierName)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("supplier_name");
        builder.Property(e => e.SupplierType)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("supplier_type");
        builder.Property(e => e.ContactPerson)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("contact_person");
        builder.Property(e => e.ContactEmail)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("contact_email");
        builder.Property(e => e.ContactPhone)
            .HasColumnType("nvarchar(12)")
            .HasColumnName("contact_phone");
        builder.Property(e => e.Address)
            .HasColumnType("nvarchar(300)")
            .HasColumnName("address");
        builder.Property(e => e.Country)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("country");
        builder.Property(e => e.City)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("city");
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasColumnName("is_active");
        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false)
            .HasColumnName("is_deleted");
        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime")
            .HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime")
            .HasColumnName("updated_at");
        #endregion
    }
}