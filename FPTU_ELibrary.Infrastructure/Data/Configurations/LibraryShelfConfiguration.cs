using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryShelfConfiguration : IEntityTypeConfiguration<LibraryShelf>
    {
        public void Configure(EntityTypeBuilder<LibraryShelf> builder)
        {
            builder.HasKey(e => e.ShelfId).HasName("PK_LibraryShelf_ShelfId");

            builder.ToTable("Library_Shelf");

            builder.Property(e => e.ShelfId).HasColumnName("shelf_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.SectionId).HasColumnName("section_id");
            builder.Property(e => e.ShelfNumber)
                .HasMaxLength(50)
                .HasColumnName("shelf_number");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.Section).WithMany(p => p.LibraryShelves)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK_LibraryShelf_SectionId");
        }
    }
}
