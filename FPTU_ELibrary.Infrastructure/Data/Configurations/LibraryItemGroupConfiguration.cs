using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryItemGroupConfiguration : IEntityTypeConfiguration<LibraryItemGroup>
{
    public void Configure(EntityTypeBuilder<LibraryItemGroup> builder)
    {
        #region Added at 15/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.GroupId).HasName("PK_LibraryItemGroup_GroupId");

        builder.ToTable("Library_Item_Group");
        
        builder.Property(e => e.GroupId).HasColumnName("group_id");
        builder.Property(e => e.AiTrainingCode)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("ai_training_code");
        builder.Property(e => e.ClassificationNumber)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("classification_number");
        builder.Property(e => e.CutterNumber)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("cutter_number");
        builder.Property(e => e.Title)
            .IsRequired()
            .HasColumnType("nvarchar(255)")
            .HasColumnName("title");
        builder.Property(e => e.SubTitle)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("sub_title");
        builder.Property(e => e.Author)
            .HasColumnType("nvarchar(200)")
            .HasColumnName("author");
        builder.Property(e => e.TopicalTerms)
            .HasColumnType("nvarchar(500)")
            .HasColumnName("topical_terms");
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
    }
}