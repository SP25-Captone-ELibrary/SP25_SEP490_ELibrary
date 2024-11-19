using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookReviewConfiguration : IEntityTypeConfiguration<BookReview>
    {
        public void Configure(EntityTypeBuilder<BookReview> builder)
        {
            builder.HasKey(e => e.ReviewId).HasName("PK_BookReview_ReviewId");

            builder.ToTable("Book_Review");

            builder.Property(e => e.ReviewId).HasColumnName("review_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.RatingValue).HasColumnName("rating_value");
            builder.Property(e => e.ReviewText)
                .HasMaxLength(2000)
                .HasColumnName("review_text");
            builder.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.BookReviews)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookReview_BookEditionId");

            builder.HasOne(d => d.User).WithMany(p => p.BookReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookReview_UserId");
        }
    }
}
