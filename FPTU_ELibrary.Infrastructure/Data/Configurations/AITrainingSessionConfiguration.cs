using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class AITrainingSessionConfiguration : IEntityTypeConfiguration<AITrainingSession>
{
    public void Configure(EntityTypeBuilder<AITrainingSession> builder)
    {
        #region Added at 06/03/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.TrainingSessionId).HasName("PK_AITrainingSession_TrainingSessionId");

        builder.ToTable("AI_Training_Session");

        builder.Property(e => e. TrainingSessionId).HasColumnName("training_session_id");
        builder.Property(e => e.Model)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("model");
        builder.Property(e => e.TrainingStatus)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("training_status");
        builder.Property(e => e.TotalTrainedItem)
            .HasDefaultValue(0)
            .HasColumnName("total_trained_item");
        builder.Property(e => e.TotalTrainedTime)
            .HasColumnName("total_trained_time");
        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("error_message");
        builder.Property(e => e.TrainDate)
            .HasColumnType("datetime")
            .HasColumnName("train_date");
        builder.Property(e => e.TrainBy)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("train_by");
        #endregion
    }
}