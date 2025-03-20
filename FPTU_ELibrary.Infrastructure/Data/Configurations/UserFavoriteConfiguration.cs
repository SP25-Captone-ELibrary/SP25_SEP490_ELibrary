using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
    {
        public void Configure(EntityTypeBuilder<UserFavorite> builder)
        {
            builder.HasKey(e => e.FavoriteId).HasName("PK_UserFavorite_FavoriteId");

            builder.ToTable("User_Favorite");

            builder.HasIndex(e => new { e.UserId, BookEditionId = e.LibraryItemId }, "UQ_UserFavorite").IsUnique();

            builder.Property(e => e.FavoriteId).HasColumnName("favorite_id");
            builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.LibraryItem).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.LibraryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorite_ItemId");

            builder.HasOne(d => d.User).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorite_UserId");

            #region Updated at: 11/03/2025 by Le Xuan Phuoc
            // builder.Property(e => e.WantsToBorrow)
            //     .HasDefaultValue(false)
            //     .HasColumnName("wants_to_borrow");
            // builder.Property(e => e.WantsToBorrowAfterRequestFailed)
            //     .HasDefaultValue(false)
            //     .HasColumnName("wants_to_borrow_after_request_failed");
            builder.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            #endregion
        }
    }
}
