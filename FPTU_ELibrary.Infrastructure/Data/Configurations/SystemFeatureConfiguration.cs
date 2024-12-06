using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class SystemFeatureConfiguration : IEntityTypeConfiguration<SystemFeature>
{
    public void Configure(EntityTypeBuilder<SystemFeature> builder)
    {
        builder.HasKey(e => e.FeatureId).HasName("PK_SystemFeature_FeatureId");

        builder.ToTable("System_Feature");

        builder.Property(e => e.FeatureId).HasColumnName("feature_id");
        builder.Property(e => e.EnglishName)
            .HasMaxLength(100)
            .HasColumnName("english_name");
        builder.Property(e => e.VietnameseName)
            .HasMaxLength(100)
            .HasColumnName("vietnamese_name");
    }
}