using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(e => e.RolePermissionId).HasName("PK_RolePermission_RolePermissionId");

        builder.ToTable("Role_Permission");
        
        builder.Property(e => e.RolePermissionId).HasColumnName("role_permission_id");
        builder.Property(e => e.RoleId).HasColumnName("role_id");
        builder.Property(e => e.FeatureId).HasColumnName("feature_id");
        builder.Property(e => e.PermissionId).HasColumnName("permission_id");
        
        builder.HasOne(e => e.Role).WithMany(e => e.RolePermissions)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("FK_RolePermission_RoleId");
        
        builder.HasOne(e => e.Feature).WithMany(e => e.RolePermissions)
            .HasForeignKey(e => e.FeatureId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("FK_RolePermission_FeatureId");
        
        builder.HasOne(e => e.Permission).WithMany(e => e.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("FK_RolePermission_PermissionId");

        #region Update at 24/12/2024 by Le Xuan Phuoc
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