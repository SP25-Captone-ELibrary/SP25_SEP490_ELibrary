using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryItemInstanceConfiguration : IEntityTypeConfiguration<LibraryItemInstance>
    {
        public void Configure(EntityTypeBuilder<LibraryItemInstance> builder)
        {
            builder.HasKey(e => e.LibraryItemInstanceId).HasName("PK_LibraryItemInstance_InstanceId");

            builder.ToTable("Library_Item_Instance");

            builder.Property(e => e.LibraryItemInstanceId).HasColumnName("library_item_instance_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.LibraryItemInstances)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryItemInstance_ItemId");

            #region Update at 23/12/2024 by Le Xuan Phuoc
            // builder.HasOne(d => d.Shelf).WithMany(p => p.BookEditionCopies)
            //     .HasForeignKey(d => d.ShelfId)
            //     .HasConstraintName("FK_BookEditionCopy_ShelfId");
            #endregion

            #region Update at 24/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");
            // builder.Property(e => e.UpdateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("update_date");

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            builder.Property(x => x.CreatedBy)
                .HasMaxLength(255) // Email address
                .HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy)
                .HasMaxLength(255) // Email address
                .HasColumnName("updated_by");
            #endregion

            #region Update at 04/01/2025 by Le Xuan Phuoc
            builder.Property(e => e.Barcode)
                .HasMaxLength(50)
                .HasColumnName("barcode");
            #endregion
        }
    }
}
