using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryItemConfiguration : IEntityTypeConfiguration<LibraryItem>
{
    public void Configure(EntityTypeBuilder<LibraryItem> builder)
    {
        #region Added at 14/01/2024 by Le Xuan Phuoc
        builder.HasKey(e => e.LibraryItemId).HasName("PK_LibraryItem_LibraryItemId");
        
        builder.ToTable("Library_Item");
        
        builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
        builder.Property(e => e.Title)
            .IsRequired()
            .HasColumnType("nvarchar(255)")
            .HasColumnName("title");
        builder.Property(e => e.SubTitle)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("sub_title");
        builder.Property(e => e.Responsibility)
            .HasColumnType("nvarchar(155)")
            .HasColumnName("responsibility");
        builder.Property(e => e.Edition)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("edition");
        builder.Property(e => e.EditionNumber).HasColumnName("edition_number");
        builder.Property(e => e.Language)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("language");
        builder.Property(e => e.OriginLanguage)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("origin_language");
        builder.Property(e => e.Summary)
            .HasColumnType("nvarchar(700)")
            .HasColumnName("summary");
        builder.Property(e => e.CoverImage)
            .HasColumnType("varchar(2048)")
            .HasColumnName("cover_image");
        builder.Property(e => e.PublicationYear).HasColumnName("publication_year");
        builder.Property(e => e.Publisher)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("publisher");
        builder.Property(e => e.PublicationPlace)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("publication_place");
        builder.Property(e => e.ClassificationNumber)
            .IsRequired()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("classification_number");
        builder.Property(e => e.CutterNumber)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("cutter_number");
        builder.Property(e => e.Isbn)
            .HasMaxLength(13)
            .HasColumnName("isbn");
        builder.Property(e => e.Ean)
            .HasMaxLength(50)
            .HasColumnName("ean");
        builder.Property(e => e.EstimatedPrice)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("estimated_price");
        builder.Property(e => e.PageCount).HasColumnName("page_count");
        builder.Property(e => e.PhysicalDetails)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("physical_details");
        builder.Property(e => e.Dimensions)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("dimensions");
        builder.Property(e => e.AccompanyingMaterial)
            .HasColumnType("nvarchar(155)")
            .HasColumnName("accompanying_material");
        builder.Property(e => e.Genres)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("genres");
        builder.Property(e => e.GeneralNote)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("general_note");
        builder.Property(e => e.BibliographicalNote)
            .HasColumnType("nvarchar(100)")
            .HasColumnName("bibliographical_note");
        builder.Property(e => e.TopicalTerms)
            .HasColumnType("nvarchar(500)")
            .HasColumnName("topical_terms");
        builder.Property(e => e.AdditionalAuthors)
            .HasColumnType("nvarchar(500)")
            .HasColumnName("additional_authors");
        builder.Property(e => e.CategoryId).HasColumnName("category_id");
        builder.Property(e => e.ShelfId).HasColumnName("shelf_id");
        builder.Property(e => e.GroupId).HasColumnName("group_id");
        builder.Property(e => e.Status)
            .HasColumnType("nvarchar(20)")
            .HasConversion<string>()
            .HasColumnName("status");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.CanBorrow).HasColumnName("can_borrow");
        builder.Property(e => e.IsTrained).HasColumnName("is_trained");
        builder.Property(e => e.TrainedAt)
            .HasColumnType("datetime")
            .HasColumnName("trained_at");
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
        
        builder.HasOne(e => e.Category).WithMany(c => c.LibraryItems)
            .HasForeignKey(c => c.CategoryId)
            .HasConstraintName("FK_LibraryItem_CategoryId");

        builder.HasOne(e => e.Shelf).WithMany(c => c.LibraryItems)
            .HasForeignKey(c => c.ShelfId)
            .HasConstraintName("FK_LibraryItem_ShelfId");
        
        builder.HasOne(e => e.LibraryItemGroup).WithMany(c => c.LibraryItems)
            .HasForeignKey(c => c.GroupId)
            .HasConstraintName("FK_LibraryItem_GroupId");
        
        // Add indexes
        builder.HasIndex(e => e.Isbn).HasDatabaseName("IX_LibraryItem_ISBN");
        builder.HasIndex(e => e.Title).HasDatabaseName("IX_LibraryItem_Title");
        #endregion
    }
}