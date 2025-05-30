﻿using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryShelfConfiguration : IEntityTypeConfiguration<LibraryShelf>
    {
        public void Configure(EntityTypeBuilder<LibraryShelf> builder)
        {
            builder.HasKey(e => e.ShelfId).HasName("PK_LibraryShelf_ShelfId");

            builder.ToTable("Library_Shelf");

            builder.Property(e => e.ShelfId).HasColumnName("shelf_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.SectionId).HasColumnName("section_id");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.Section).WithMany(p => p.LibraryShelves)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryShelf_SectionId");

            #region Update at 28/02/2025 by Le Xuan Phuoc
            builder.Property(e => e.ShelfNumber)
                .HasMaxLength(50)
                .HasColumnName("shelf_number");
            
            builder.Property(e => e.EngShelfName)
                .HasMaxLength(155)
                .HasColumnName("eng_shelf_name");
            
            builder.Property(e => e.VieShelfName)
                .HasMaxLength(155)
                .HasColumnName("vie_shelf_name");
            
            builder.Property(e => e.ClassificationNumberRangeFrom)
                .HasDefaultValue(0)
                .HasColumnType("decimal(10,4)")
                .HasColumnName("classification_number_range_from");
            
            builder.Property(e => e.ClassificationNumberRangeTo)
                .HasDefaultValue(0)
                .HasColumnType("decimal(10,4)")
                .HasColumnName("classification_number_range_to");
            #endregion
        }
    }
}
