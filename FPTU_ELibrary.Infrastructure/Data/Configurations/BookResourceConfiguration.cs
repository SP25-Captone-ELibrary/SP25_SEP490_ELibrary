using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookResourceConfiguration : IEntityTypeConfiguration<BookResource>
    {
        public void Configure(EntityTypeBuilder<BookResource> builder)
        {
            builder.HasKey(e => e.ResourceId).HasName("PK_BookResource_BookResourceId");

            builder.ToTable("Book_Resource");

            builder.Property(e => e.ResourceId).HasColumnName("resource_id");
            builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.FileFormat)
                .HasMaxLength(50)
                .HasColumnName("file_format");
            builder.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            builder.Property(e => e.ProviderMetadata)
                .HasMaxLength(1000)
                .HasColumnName("provider_metadata");
            builder.Property(e => e.ProviderPublicId)
                .HasMaxLength(255)
                .HasColumnName("provider_public_id");
            builder.Property(e => e.ResourceSize)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("resource_size");
            builder.Property(e => e.ResourceType)
                .HasMaxLength(50)
                .HasColumnName("resource_type");
            builder.Property(e => e.ResourceUrl)
                .HasMaxLength(2048)
                .HasColumnName("resource_url");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.BookEdition).WithMany(p => p.BookResources)
                .HasForeignKey(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookResource_BookEditionId");

            builder.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BookResources)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookResource_CreatedBy");
        }
    }
}
