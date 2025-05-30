﻿using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryResourceConfiguration : IEntityTypeConfiguration<LibraryResource>
    {
        public void Configure(EntityTypeBuilder<LibraryResource> builder)
        {
            builder.HasKey(e => e.ResourceId).HasName("PK_LibraryResource_ResourceId");

            builder.ToTable("Library_Resource");

            builder.Property(e => e.ResourceId).HasColumnName("resource_id");
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
            builder.Property(e => e.ResourceTitle)
                .HasColumnType("nvarchar(255)")
                .HasColumnName("resource_title");
            builder.Property(e => e.ResourceType)
                .HasMaxLength(50)
                .HasColumnName("resource_type");
            builder.Property(e => e.S3OriginalName)
                .HasMaxLength(50)
                .HasColumnName("s3_original_name");
            builder.Property(e => e.ResourceUrl)
                .HasMaxLength(2048)
                .HasColumnName("resource_url");
            builder.Property(e => e.DefaultBorrowDurationDays)
                .HasColumnName("default_borrow_duration_days");
            builder.Property(e => e.BorrowPrice)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("borrow_price");

            #region Update at 23/12/2024 by Le Xuan Phuoc

            // builder.Property(e => e.BookEditionId).HasColumnName("book_edition_id");
            // builder.HasOne(d => d.BookEdition).WithMany(p => p.BookResources)
            //     .HasForeignKey(d => d.BookEditionId)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BookResource_BookEditionId");

            // builder.Property(e => e.BookId).HasColumnName("book_id");
            // builder.HasOne(d => d.Book).WithMany(p => p.BookResources)
            //     .HasForeignKey(d => d.BookId)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BookResource_BookId");

            #endregion

            #region Update at 24/12/2024 by Le Xuan Phuoc

            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");
            // builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            // builder.Property(e => e.UpdateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("update_date");
            //
            // builder.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BookResources)
            //     .HasForeignKey(d => d.CreatedBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_BookResource_CreatedBy");

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

            #region Update at 25/12/2024 by Le Xuan Phuoc

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");

            #endregion
        /*
         # region Update at 15/03/2025 by Nguyen Vu Quang Huy

                     builder.Property(e => e.SubPublicId)
                         .HasMaxLength(1024)
                         .HasColumnName("sub_public_id");
                     #endregion
                     */
        }
    }
}