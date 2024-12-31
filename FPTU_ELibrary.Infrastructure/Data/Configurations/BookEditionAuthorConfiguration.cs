using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookEditionAuthorConfiguration : IEntityTypeConfiguration<BookEditionAuthor>
    {
        public void Configure(EntityTypeBuilder<BookEditionAuthor> builder)
        {
            #region Update at 17/12/2024 by Le Xuan Phuoc
            builder.HasKey(e => e.BookEditionAuthorId).HasName("PK_BookAuthorEdition_BookEditionAuthorId");

            builder.ToTable("Book_Edition_Author");

            builder.Property(e => e.BookEditionAuthorId).HasColumnName("book_edition_author_id");
            builder.Property(e => e.AuthorId).HasColumnName("author_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");

            builder.HasOne(d => d.Author).WithMany(p => p.BookEditionAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookEditionAuthor_AuthorId");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.BookEditionAuthors)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookEditionAuthor_BookId");
            #endregion
            
            #region Update at 30/12/2024 by Le Xuan Phuoc
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
