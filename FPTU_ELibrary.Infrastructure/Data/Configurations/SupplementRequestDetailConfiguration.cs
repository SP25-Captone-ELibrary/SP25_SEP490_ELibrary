using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class SupplementRequestDetailConfiguration : IEntityTypeConfiguration<SupplementRequestDetail>
{
    public void Configure(EntityTypeBuilder<SupplementRequestDetail> builder)
    {
        #region Added at 07/04/2025
        builder.HasKey(e => e.SupplementRequestDetailId).HasName("PK_SupplementRequestDetail_SupplementRequestDetailId");
        
        builder.ToTable("Supplement_Request_Detail");

        builder.Property(e => e.SupplementRequestDetailId).HasColumnName("supplement_request_detail_id");
        builder.Property(e => e.Title)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("title");
        builder.Property(e => e.Author)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("author");
        builder.Property(e => e.Publisher)
            .HasColumnType("nvarchar(155)")
            .HasColumnName("publisher");
        builder.Property(e => e.PublishedDate)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("published_date");
        builder.Property(e => e.Description)
            .HasColumnType("nvarchar(3000)")
            .HasColumnName("description");
        builder.Property(e => e.Isbn)
            .HasMaxLength(13)
            .HasColumnName("isbn");
        builder.Property(e => e.Language)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("language");
        builder.Property(e => e.PageCount).HasColumnName("page_count");
        builder.Property(e => e.AverageRating).HasColumnName("average_rating");
        builder.Property(e => e.RatingsCount).HasColumnName("ratings_count");
        builder.Property(e => e.Dimensions)
            .HasColumnType("nvarchar(155)")
            .HasColumnName("dimensions");
        builder.Property(e => e.EstimatedPrice)
            .HasColumnType("decimal(10,2)")
            .HasColumnName("estimated_price");
        builder.Property(e => e.Categories)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("categories");
        builder.Property(e => e.CoverImageLink)
            .HasColumnType("varchar(2048)")
            .HasColumnName("cover_image");
        builder.Property(e => e.PreviewLink)
            .HasColumnType("varchar(2048)")
            .HasColumnName("preview_link");
        builder.Property(e => e.InfoLink)
            .HasColumnType("varchar(2048)")
            .HasColumnName("info_link");
        
        builder.Property(e => e.RelatedLibraryItemId).HasColumnName("related_library_item_id");
        builder.HasOne(e => e.RelatedLibraryItem).WithMany(e => e.SupplementRequestDetails)
            .HasForeignKey(e => e.RelatedLibraryItemId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_SupplementRequestDetail_RelatedLibraryItemId");
        
        builder.Property(e => e.TrackingId).HasColumnName("tracking_id");
        builder.HasOne(e => e.WarehouseTracking).WithMany(e => e.SupplementRequestDetails)
            .HasForeignKey(e => e.TrackingId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_SupplementRequestDetail_TrackingId");
        #endregion
    }
}