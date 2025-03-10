using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class AITrainingDetailConfiguration : IEntityTypeConfiguration<AITrainingDetail>
{
    public void Configure(EntityTypeBuilder<AITrainingDetail> builder)
    {
        #region Added at 06/03/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.TrainingDetailId).HasName("PK_AITrainingDetail_TrainingDetailId");

        builder.ToTable("AI_Training_Detail");

        builder.Property(e => e. TrainingDetailId).HasColumnName("training_detail_id");

        builder.Property(e => e.TrainingSessionId).HasColumnName("training_session_id");
        builder.HasOne(e => e.TrainingSession).WithMany(p => p.TrainingDetails)
            .HasForeignKey(e => e.TrainingSessionId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_AITrainingDetail_TrainingSessionId");
        
        builder.Property(e => e.LibraryItemId).HasColumnName("library_item_id");
        builder.HasOne(e => e.LibraryItem).WithMany(p => p.TrainingDetails)
            .HasForeignKey(e => e.LibraryItemId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_AITrainingDetail_LibraryItemId");
        #endregion
    }
}