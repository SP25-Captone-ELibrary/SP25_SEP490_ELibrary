using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibrarySectionConfiguration : IEntityTypeConfiguration<LibrarySection>
    {
        public void Configure(EntityTypeBuilder<LibrarySection> builder)
        {
            builder.HasKey(e => e.SectionId).HasName("PK_LibrarySection_SectionId");

            builder.ToTable("Library_Section");

            builder.Property(e => e.SectionId).HasColumnName("section_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.SectionName)
                .HasMaxLength(100)
                .HasColumnName("section_name");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");
            builder.Property(e => e.ZoneId).HasColumnName("zone_id");

            builder.HasOne(d => d.Zone).WithMany(p => p.LibrarySections)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibrarySection_ZoneId");
        }
    }
}
