using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
    {
        public void Configure(EntityTypeBuilder<BookCategory> builder)
        {
            builder.HasKey(e => e.CategoryId).HasName("PK_BookCategory_CategoryId");

            builder.ToTable("Book_Category");

            builder.Property(e => e.CategoryId).HasColumnName("category_id");
            builder.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            builder.Property(e => e.EnglishName)
                .HasMaxLength(155)
                .HasColumnName("english_name");
			builder.Property(e => e.VietnameseName)
				.HasMaxLength(155)
				.HasColumnName("vietnamese_name");

            #region Add IsDeleted Field Quang Huy 16-12-2024
            builder.Property(e => e.IsDeleted)
                .HasMaxLength(155)
                .HasColumnName("is_deleted");
            #endregion
        }
    }
}
