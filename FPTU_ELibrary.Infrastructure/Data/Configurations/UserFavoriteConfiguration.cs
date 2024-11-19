using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
    {
        public void Configure(EntityTypeBuilder<UserFavorite> builder)
        {
            builder.HasKey(e => e.FavoriteId).HasName("PK_UserFavorites_FavoriteId");

            builder.ToTable("User_Favorites");

            builder.HasIndex(e => new { e.UserId, e.BookEditionId }, "UQ_UserFavorites").IsUnique();

            builder.Property(e => e.FavoriteId).HasColumnName("favorite_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.UserId).HasColumnName("user_id");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorites_BookEditionId");

            builder.HasOne(d => d.User).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorites_UserId");
        }
    }
}
