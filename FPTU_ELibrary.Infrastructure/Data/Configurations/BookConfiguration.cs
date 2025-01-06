using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.HasKey(e => e.BookId).HasName("PK_Book_BookId");

            builder.ToTable("Book");

            builder.Property(e => e.BookId).HasColumnName("book_id");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.Summary)
                .HasMaxLength(2000)
                .HasColumnName("summary");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            #region Update at 20/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.CanBorrow)
            //     .HasDefaultValue(true)
            //     .HasColumnName("can_borrow");
            
            // builder.Property(e => e.CategoryId).HasColumnName("category_id");
            // builder.HasOne(d => d.Category).WithMany(p => p.Books)
            //     .HasForeignKey(d => d.CategoryId)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_Book_CategoryId");
            
            builder.Property(e => e.SubTitle)
                .HasMaxLength(100)
                .HasColumnName("sub_title");
            #endregion

            #region Update at 24/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.CreateBy).HasColumnName("create_by");
            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");
            //
            // builder.Property(e => e.UpdatedDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("updated_date");
            //
            // builder.HasOne(d => d.CreateByNavigation).WithMany(p => p.BookCreateByNavigations)
            //     .HasForeignKey(d => d.CreateBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_Book_CreateBy");
            // builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            // builder.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.BookUpdatedByNavigations)
            //     .HasForeignKey(d => d.UpdatedBy)
            //     .HasConstraintName("FK_Book_UpdateBy");

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
            
            #region Update 2/1/2025 update AI field
            builder.Property(e => e.BookCodeForAITraining)
                .HasColumnName("book_code_for_ai");
            #endregion

            #region Update at 01/02/2025 by Le Xuan Phuoc
            // builder.Property(e => e.IsDraft)
            //     .HasDefaultValue(true)
            //     .HasColumnName("is_draft");
            // builder.Property(e => e.IsTrained)
            //     .HasColumnName("is_trained")
            //     .HasDefaultValue(false);
            // builder.Property(e => e.TrainedDay)
            //     .HasColumnType("datetime")
            //     .HasColumnName("trained_day");
            #endregion

            #region Update at 04/01/2025 by Le Xuan Phuoc
            builder.Property(e => e.BookCode)
                .HasMaxLength(100)
                .HasColumnName("book_code");
            #endregion
        }
    }
}
