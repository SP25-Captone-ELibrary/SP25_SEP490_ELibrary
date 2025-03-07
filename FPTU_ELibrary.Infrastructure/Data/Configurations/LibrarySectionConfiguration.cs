using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");
            builder.Property(e => e.ZoneId).HasColumnName("zone_id");

            builder.HasOne(d => d.Zone).WithMany(p => p.LibrarySections)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibrarySection_ZoneId");

            #region Update at 28/02/2025 by Le Xuan Phuoc
            builder.Property(e => e.ClassificationNumberRangeFrom)
                .HasDefaultValue(0)
                .HasColumnType("decimal(10,4)")
                .HasColumnName("classification_number_range_from");
            
            builder.Property(e => e.ClassificationNumberRangeTo)
                .HasDefaultValue(0)
                .HasColumnType("decimal(10,4)")
                .HasColumnName("classification_number_range_to");
            
            builder.Property(e => e.EngSectionName)
                .HasMaxLength(100)
                .HasColumnName("eng_section_name");
            
            builder.Property(e => e.VieSectionName)
                .HasMaxLength(100)
                .HasColumnName("vie_section_name");
            #endregion

            #region Update at 01/03/2025 by Le Xuan Phuoc
            builder.Property(e => e.IsChildrenSection)
                .HasDefaultValue(false)
                .HasColumnName("is_children_section");
            
            builder.Property(e => e.IsReferenceSection)
                .HasDefaultValue(false)
                .HasColumnName("is_reference_section");
            
            builder.Property(e => e.ShelfPrefix)
                .HasColumnType("nvarchar(10)")
                .HasColumnName("shelf_prefix");
            #endregion

            #region Update at 03/03/2025
            builder.Property(e => e.IsJournalSection)
                .HasDefaultValue(false)
                .HasColumnName("is_journal_section");
            #endregion
        }
    }
}
