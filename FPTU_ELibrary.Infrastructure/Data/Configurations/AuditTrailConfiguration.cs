using FPTU_ELibrary.Domain.Converters;
using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        #region Update at 24/12/2024
        builder.HasKey(e => e.AuditTrailId).HasName("PK_AuditTrail_AuditTrailId");

        builder.ToTable("Audit_Trail");

        builder.Property(e => e.AuditTrailId).HasColumnName("audit_trail_id");
        builder.Property(e => e.Email)
            .HasMaxLength(255)
            .HasColumnName("email");
        builder.Property(e => e.EntityId)
            .HasMaxLength(50) 
            .HasColumnName("entity_id");
        builder.Property(e => e.EntityName)
            .HasMaxLength(100)
            .HasColumnName("entity_name");
        builder.Property(e => e.DateUtc)
            .HasColumnType("datetime")
            .HasColumnName("date_utc");
        builder.Property(e => e.TrailType)
            .HasConversion<string>()
            .HasColumnName("trail_type");
        builder.Property(e => e.ChangedColumns)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("changed_columns");
        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(2500)")
            .HasColumnName("old_values")
            .HasConversion(new DictionaryToJsonConverter());
            // Mark as read only after saved
            // .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(2500)")
            .HasColumnName("new_values")
            .HasConversion(new DictionaryToJsonConverter());
            // Mark as read only after saved
            // .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        #endregion
    }
}