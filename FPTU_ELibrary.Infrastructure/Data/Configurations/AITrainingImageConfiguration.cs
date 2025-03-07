using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class AITrainingImageConfiguration : IEntityTypeConfiguration<AITrainingImage>
{
    public void Configure(EntityTypeBuilder<AITrainingImage> builder)
    {
        #region Added at 06/03/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.TrainingImageId).HasName("PK_AITrainingImage_TrainingImageId");

        builder.ToTable("AI_Training_Image");

        builder.Property(e => e. TrainingImageId).HasColumnName("training_image_id");
        
        builder.Property(e => e.TrainingDetailId).HasColumnName("training_detail_id");
        builder.HasOne(e => e.TrainingDetail).WithMany(p => p.TrainingImages)
            .HasForeignKey(e => e.TrainingDetailId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_AITrainingImage_TrainingDetailId");
        #endregion
    }
}