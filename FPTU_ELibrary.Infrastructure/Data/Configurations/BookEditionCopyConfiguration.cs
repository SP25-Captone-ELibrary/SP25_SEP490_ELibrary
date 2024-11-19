using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookEditionCopyConfiguration : IEntityTypeConfiguration<BookEditionCopy>
    {
        public void Configure(EntityTypeBuilder<BookEditionCopy> builder)
        {
            builder.HasKey(e => e.BookEditionCopyId).HasName("PK_BookEditionCopy_BookEditionCopyId");

            builder.ToTable("Book_Edition_Copy");

            builder.Property(e => e.BookEditionCopyId).HasColumnName("book_edition_copy_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.ShelfId).HasColumnName("shelf_id");
            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.BookEditionCopies)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookEditionCopy_BookEditionId");

            builder.HasOne(d => d.Shelf).WithMany(p => p.BookEditionCopies)
                .HasForeignKey(d => d.ShelfId)
                .HasConstraintName("FK_BookEditionCopy_ShelfId");
        }
    }
}
