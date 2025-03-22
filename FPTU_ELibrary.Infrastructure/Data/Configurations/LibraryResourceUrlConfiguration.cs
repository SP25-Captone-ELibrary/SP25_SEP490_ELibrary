using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryResourceUrlConfiguration: IEntityTypeConfiguration<LibraryResourceUrl>
{
    public void Configure(EntityTypeBuilder<LibraryResourceUrl> builder)
    {
        builder.HasKey(e => e.LibraryResourceUrlId).HasName("PK_LibraryResourceUrl_LibraryResourceUrlId");

        builder.ToTable("Library_Resource_Url");

        builder.Property(e => e.LibraryResourceUrlId)
            .HasColumnName("library_resource_url_id");

        builder.Property(e => e.LibraryResourceId)
            .IsRequired()
            .HasColumnName("resource_id");

        builder.Property(e => e.Url)
            .IsRequired()
            .HasMaxLength(2048)
            .HasColumnName("url");

        builder.Property(e => e.PartNumber)
            .HasColumnName("part_number");

        builder.HasOne(e => e.LibraryResource)
            .WithMany(r => r.LibraryResourceUrls)
            .HasForeignKey(e => e.LibraryResourceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LibraryResourceUrl_ResourceId");
    }
}