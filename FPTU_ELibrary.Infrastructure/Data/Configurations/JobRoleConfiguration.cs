using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class JobRoleConfiguration : IEntityTypeConfiguration<JobRole>
    {
        public void Configure(EntityTypeBuilder<JobRole> builder)
        {
            builder.HasKey(e => e.JobRoleId).HasName("PK_JobRole_JobRoleId");

            builder.ToTable("Job_Role");

            builder.Property(e => e.JobRoleId).HasColumnName("job_role_id");
            builder.Property(e => e.EnglishName)
                .HasMaxLength(100)
                .HasColumnName("english_name");
			builder.Property(e => e.VietnameseName)
				.HasMaxLength(100)
				.HasColumnName("vietnamese_name");
        }
    }
}
