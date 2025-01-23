using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    internal class AuthorConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.HasKey(e => e.AuthorId).HasName("PK_Author_AuthorId");

            builder.ToTable("Author");

            builder.Property(e => e.AuthorId).HasColumnName("author_id");
            builder.Property(e => e.AuthorCode)
                .HasMaxLength(20)
                .HasColumnName("author_code");
            builder.Property(e => e.AuthorImage)
                .HasMaxLength(2048)
                .IsUnicode(false)
                .HasColumnName("author_image");
            builder.Property(e => e.Biography)
                .HasMaxLength(3000)
                .HasColumnName("biography");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.DateOfDeath)
                .HasColumnType("datetime")
                .HasColumnName("date_of_death");
            builder.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("dob");
            // builder.Property(e => e.FirstName)
            //     .HasMaxLength(100)
            //     .HasColumnName("first_name");
            // builder.Property(e => e.LastName)
            //     .HasMaxLength(100)
            //     .HasColumnName("last_name");
            builder.Property(e => e.Nationality)
                .HasMaxLength(100)
                .HasColumnName("nationality");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            #region Update at 16/12/2024 by Le Xuan Phuoc
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            #endregion
        }
    }
}
