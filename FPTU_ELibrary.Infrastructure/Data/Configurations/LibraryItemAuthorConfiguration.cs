using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryItemAuthorConfiguration : IEntityTypeConfiguration<LibraryItemAuthor>
    {
        public void Configure(EntityTypeBuilder<LibraryItemAuthor> builder)
        {
            #region Update at 17/12/2024 by Le Xuan Phuoc
            builder.HasKey(e => e.LibraryItemAuthorId).HasName("PK_LibraryItemAuthor_LibraryItemAuthorId");

            builder.ToTable("Library_Item_Author");

            builder.Property(e => e.LibraryItemAuthorId).HasColumnName("library_item_author_id");
            builder.Property(e => e.AuthorId).HasColumnName("author_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");

            builder.HasOne(d => d.Author).WithMany(p => p.LibraryItemAuthors)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryItemAuthor_AuthorId");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.LibraryItemAuthors)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryItemAuthor_LibraryItemId");
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
