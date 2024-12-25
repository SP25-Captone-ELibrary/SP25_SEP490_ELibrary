using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class SystemRoleConfiguration : IEntityTypeConfiguration<SystemRole>
    {
        public void Configure(EntityTypeBuilder<SystemRole> builder)
        {
            builder.HasKey(e => e.RoleId).HasName("PK_SystemRole_RoleId");

            builder.ToTable("System_Role");

            builder.Property(e => e.RoleId).HasColumnName("role_id");
            builder.Property(e => e.RoleType)
                .HasMaxLength(50)
                .HasColumnName("role_type");
            builder.Property(e => e.EnglishName)
                .HasMaxLength(100)
                .HasColumnName("english_name");
            builder.Property(e => e.VietnameseName)
                .HasMaxLength(100)
                .HasColumnName("vietnamese_name");

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
}
