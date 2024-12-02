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
            builder.Property(e => e.EnglishName)
                .HasMaxLength(100)
                .HasColumnName("english_name");
            builder.Property(e => e.VietnameseName)
                .HasMaxLength(100)
                .HasColumnName("vietnamese_name");
        }
    }
}
