using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        #region Update at 20/12/2024 by Le Xuan Phuoc
        builder.HasKey(e => e.BookCategoryId).HasName("PK_BookCategory_BookCategoryId");
        
        builder.ToTable("Book_Category");

        builder.Property(e => e.BookCategoryId).HasColumnName("book_category_id");
        builder.Property(e => e.BookId).HasColumnName("book_id");
        builder.Property(e => e.CategoryId).HasColumnName("category_id");

        builder.HasOne(d => d.Book).WithMany(p => p.BookCategories)
            .HasForeignKey(d => d.BookId)
            .HasConstraintName("FK_BookCategory_BookId");
        
        builder.HasOne(d => d.Category).WithMany(p => p.BookCategories)
            .HasForeignKey(d => d.CategoryId)
            .HasConstraintName("FK_BookCategory_CategoryId");
        
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