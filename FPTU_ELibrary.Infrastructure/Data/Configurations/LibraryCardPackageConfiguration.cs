using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryCardPackageConfiguration : IEntityTypeConfiguration<LibraryCardPackage>
{
    public void Configure(EntityTypeBuilder<LibraryCardPackage> builder)
    {
        #region Added at 05/02/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.LibraryCardPackageId).HasName("PK_LibraryCardPackage_LibraryCardPackageId");

        builder.ToTable("Library_Card_Package");
        
        builder.Property(e => e.LibraryCardPackageId).HasColumnName("library_card_package_id");
        builder.Property(e => e.PackageName)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("package_name");
        builder.Property(e => e.Price)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("price");
        builder.Property(e => e.DurationInMonths).HasColumnName("duration_in_months");
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.CreatedAt)
            .HasColumnType("datetime")
            .HasColumnName("created_at");
        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(1000)")
            .HasColumnName("description");
        #endregion
    }
}