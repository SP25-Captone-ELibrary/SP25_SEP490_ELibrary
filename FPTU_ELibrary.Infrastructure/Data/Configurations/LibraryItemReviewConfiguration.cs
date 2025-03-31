using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryItemReviewConfiguration : IEntityTypeConfiguration<LibraryItemReview>
    {
        public void Configure(EntityTypeBuilder<LibraryItemReview> builder)
        {
            builder.HasKey(e => e.ReviewId).HasName("PK_LibraryItemReview_ReviewId");

            builder.ToTable("Library_Item_Review");

            builder.Property(e => e.ReviewId).HasColumnName("review_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.RatingValue).HasColumnName("rating_value");
            builder.Property(e => e.ReviewText)
                .HasMaxLength(1000)
                .HasColumnName("review_text");
            builder.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.LibraryItemReviews)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryItemReview_ItemId");

            builder.HasOne(d => d.User).WithMany(p => p.LibraryItemReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryItemReview_UserId");
        }
    }
}
