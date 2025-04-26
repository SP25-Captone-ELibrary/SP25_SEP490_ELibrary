using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class FinePolicyConfiguration : IEntityTypeConfiguration<FinePolicy>
    {
        public void Configure(EntityTypeBuilder<FinePolicy> builder)
        {
            builder.HasKey(e => e.FinePolicyId).HasName("PK_FinePolicy_FinePolicyId");

            builder.ToTable("Fine_Policy");

            builder.Property(e => e.FinePolicyId).HasColumnName("fine_policy_id");
            builder.Property(e => e.ConditionType)
                .HasColumnType("nvarchar(100)")
                .HasConversion<string>()
                .HasColumnName("condition_type");
            builder.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            
            #region Updated at 16/01/2025 by Le Xuan Phuoc
            builder.Property(e => e.FinePolicyTitle)
                .HasColumnType("nvarchar(255)")
                .HasColumnName("fine_policy_title");
            #endregion
            
            #region Updated at 15/03/2025 by Le Xuan Phuoc
            builder.Property(e => e.FineAmountPerDay)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("fine_amount_per_day");
            builder.Property(e => e.FixedFineAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("fixed_fine_amount");
            #endregion

            #region Updated at 25/04/2025 by Le Xuan Phuoc
            builder.Property(e => e.MinDamagePct)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("min_damage_pct");
            builder.Property(e => e.MaxDamagePct)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("max_damage_pct");
            builder.Property(e => e.ChargePct)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("charge_pct");
            builder.Property(e => e.ProcessingFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("processing_fee");
            builder.Property(e => e.DailyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("daily_rate");
            #endregion
        }
    }
}
