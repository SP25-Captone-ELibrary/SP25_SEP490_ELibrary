using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookEditionConfiguration : IEntityTypeConfiguration<BookEdition>
    {
        public void Configure(EntityTypeBuilder<BookEdition> builder)
        {
            builder.HasKey(e => e.BookEditionId).HasName("PK_BookEdition_BookEditionId");

            builder.ToTable("Book_Edition");

            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.BookId).HasColumnName("book_id");
            builder.Property(e => e.CoverImage)
                .HasMaxLength(2048)
                .HasColumnName("cover_image");
            builder.Property(e => e.EditionTitle)
                .HasMaxLength(255)
                .HasColumnName("edition_title");
            builder.Property(e => e.EditionNumber).HasColumnName("edition_number");
            builder.Property(e => e.Format)
                .HasMaxLength(50)
                .HasColumnName("format");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.Isbn)
                .HasMaxLength(13)
                .HasColumnName("isbn");
            builder.Property(e => e.Language)
                .HasMaxLength(50)
                .HasColumnName("language");
            builder.Property(e => e.PageCount).HasColumnName("page_count");
            builder.Property(e => e.PublicationYear).HasColumnName("publication_year");
            builder.Property(e => e.Publisher)
                .HasMaxLength(255)
                .HasColumnName("publisher");

            builder.HasOne(d => d.Book).WithMany(p => p.BookEditions)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookEdition_Book");
            

            #region Update at 20/12/2024 by Le Xuan Phuoc
            builder.Property(e => e.CanBorrow)
                .HasDefaultValue(false)
                .HasColumnName("can_borrow");
            builder.Property(e => e.EditionSummary)
                .HasMaxLength(500)
                .HasColumnName("edition_summary");
            #endregion

            #region Update at 23/12/2024 by Le Xuan Phuoc
            builder.Property(e => e.EstimatedPrice)
                .HasColumnType("decimal(10,0)")
                .HasColumnName("estimated_price");
            
            builder.HasOne(d => d.Shelf).WithMany(p => p.BookEditions)
                .HasForeignKey(d => d.ShelfId)
                .HasConstraintName("FK_BookEdition_ShelfId");
            #endregion

            #region Update at 24/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.CreateBy).HasColumnName("create_by");
            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");
            // builder.Property(e => e.UpdatedDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("updated_date");
            // builder.HasOne(d => d.CreateByNavigation).WithMany(p => p.BookEditions)
            //     .HasForeignKey(d => d.CreateBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BookEdition_CreateBy");
            
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
