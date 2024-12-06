using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class SystemPermissionConfiguration : IEntityTypeConfiguration<SystemPermission>
{
    public void Configure(EntityTypeBuilder<SystemPermission> builder)
    {
        builder.HasKey(e => e.PermissionId).HasName("PK_SystemPermission_PermissionId");

        builder.ToTable("System_Permission");

        builder.Property(e => e.PermissionId).HasColumnName("permission_id");
        builder.Property(e => e.PermissionLevel).HasColumnName("permission_level");
        builder.Property(e => e.EnglishName)
            .HasMaxLength(100)
            .HasColumnName("english_name");
        builder.Property(e => e.VietnameseName)
            .HasMaxLength(100)
            .HasColumnName("vietnamese_name");
    }
}