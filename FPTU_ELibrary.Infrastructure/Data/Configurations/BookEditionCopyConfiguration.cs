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
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.BookEditionCopies)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookEditionCopy_BookEditionId");

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
        }
    }
}
