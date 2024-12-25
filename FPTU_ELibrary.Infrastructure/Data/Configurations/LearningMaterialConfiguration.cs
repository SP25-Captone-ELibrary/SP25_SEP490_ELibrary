using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LearningMaterialConfiguration : IEntityTypeConfiguration<LearningMaterial>
    {
        public void Configure(EntityTypeBuilder<LearningMaterial> builder)
        {
            builder.HasKey(e => e.LearningMaterialId).HasName("PK_LearningMaterial_LearningMaterialId");

            builder.ToTable("Learning_Material");

            builder.Property(e => e.LearningMaterialId).HasColumnName("learning_material_id");
            builder.Property(e => e.AvailableQuantity)
                .HasDefaultValue(1)
                .HasColumnName("available_quantity");
            builder.Property(e => e.Condition)
                .HasMaxLength(100)
                .HasColumnName("condition");
            builder.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            builder.Property(e => e.Manufacturer)
                .HasMaxLength(255)
                .HasColumnName("manufacturer");
            builder.Property(e => e.MaterialType)
                .HasMaxLength(100)
                .HasColumnName("material_type");
            builder.Property(e => e.ShelfId).HasColumnName("shelf_id");
            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(e => e.TotalQuantity)
                .HasDefaultValue(1)
                .HasColumnName("total_quantity");
            builder.Property(e => e.WarrantyPeriod).HasColumnName("warranty_period");
            builder.HasOne(d => d.Shelf).WithMany(p => p.LearningMaterials)
                .HasForeignKey(d => d.ShelfId)
                .HasConstraintName("FK_LearningMaterial_ShelfId");

            //builder.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.LearningMaterialUpdatedByNavigations)
            //    .HasForeignKey(d => d.UpdatedBy)
            //    .HasConstraintName("FK_LearningMaterial_UpdateBy");

            #region Update at 24/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.CreateBy).HasColumnName("create_by");
            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");
            // builder.HasOne(d => d.CreateByNavigation).WithMany(p => p.LearningMaterialCreateByNavigations)
            //     .HasForeignKey(d => d.CreateBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_LearningMaterial_CreateBy");
            //builder.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.LearningMaterialUpdatedByNavigations)
            //    .HasForeignKey(d => d.UpdatedBy)
            //    .HasConstraintName("FK_LearningMaterial_UpdateBy");
            
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
}
