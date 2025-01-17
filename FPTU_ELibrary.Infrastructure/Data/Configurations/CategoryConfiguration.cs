using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(e => e.CategoryId).HasName("PK_Category_CategoryId");

            builder.ToTable("Category");

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

            #region Update at 14/01/2025 by Le Xuan Phuoc
            builder.Property(e => e.Prefix)
                .HasColumnType("nvarchar(20)")
                .HasColumnName("prefix");
            #endregion
		}
    }
}
