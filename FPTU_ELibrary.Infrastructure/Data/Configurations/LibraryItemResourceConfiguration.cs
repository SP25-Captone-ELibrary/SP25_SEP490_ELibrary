using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryItemResourceConfiguration : IEntityTypeConfiguration<LibraryItemResource>
{
    public void Configure(EntityTypeBuilder<LibraryItemResource> builder)
    {
        #region Added at 14/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.LibraryItemResourceId).HasName("PK_LibraryItemResource");

        builder.ToTable("Library_Item_Resource");
        
        builder.Property(e => e.LibraryItemResourceId).HasColumnName("library_item_resource_id");
        builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
        builder.Property(e => e.ResourceId).HasColumnName("resource_id");

        builder.HasOne(e => e.LibraryItem).WithMany(e => e.LibraryItemResources)
            .HasForeignKey(e => e.LibraryItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LibraryItemResource_LibraryItemId");
        
        builder.HasOne(e => e.LibraryResource).WithMany(e => e.LibraryItemResources)
            .HasForeignKey(e => e.ResourceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LibraryItemResource_ResourceId");
        #endregion
    }
}