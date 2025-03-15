using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryItemInventoryConfiguration : IEntityTypeConfiguration<LibraryItemInventory>
    {
        public void Configure(EntityTypeBuilder<LibraryItemInventory> builder)
        {
            builder.HasKey(e => e.LibraryItemId).HasName("PK_LibraryItemInventory_LibraryItemId");

            builder.ToTable("Library_Item_Inventory");

            builder.Property(e => e.LibraryItemId)
                .ValueGeneratedNever()
                .HasColumnName("library_item_id");
            builder.Property(e => e.AvailableUnits)
                .HasDefaultValue(0)
                .HasColumnName("available_units");
            builder.Property(e => e.RequestUnits)
                .HasDefaultValue(0)
                .HasColumnName("request_units");
            builder.Property(e => e.BorrowedUnits)
                .HasDefaultValue(0)
                .HasColumnName("borrowed_units");
            builder.Property(e => e.ReservedUnits)
                .HasDefaultValue(0)
                .HasColumnName("reserved_units");
            builder.Property(e => e.TotalUnits)
                .HasDefaultValue(0)
                .HasColumnName("total_units");

            builder.HasOne(d => d.LibraryItem).WithOne(p => p.LibraryItemInventory)
                .HasForeignKey<LibraryItemInventory>(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_LibraryItemInventory_LibraryItemId");

            #region Updated at 15/03/2025 by Le Xuan Phuoc
            builder.Property(e => e.LostUnits)
                .HasDefaultValue(0)
                .HasColumnName("lost_units");
            #endregion
        }
    }
}
