using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class SystemMessageConfiguration : IEntityTypeConfiguration<SystemMessage>
{
    public void Configure(EntityTypeBuilder<SystemMessage> builder)
    {
        #region Created at: 03-12-2024 by Le Xuan Phuoc
        builder.HasKey(e => e.MsgId).HasName("PK_SystemMessage_MsgId");

        builder.ToTable("System_Message");
        
        builder.Property(e => e.MsgId)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("msg_id");
        builder.Property(e => e.MsgContent)
            .HasMaxLength(1500)
            .HasColumnName("msg_content");
        builder.Property(e => e.Vi)
            .HasMaxLength(1500)
            .HasColumnName("VI");
        builder.Property(e => e.En)
            .HasMaxLength(1500)
            .HasColumnName("EN");
        builder.Property(e => e.Ru)
            .HasMaxLength(1500)
            .HasColumnName("RU");
        builder.Property(e => e.Ja)
            .HasMaxLength(1500)
            .HasColumnName("JA");
        builder.Property(e => e.Ko)
            .HasMaxLength(1500)
            .HasColumnName("KO");
        builder.Property(e => e.CreateDate)
            .HasColumnType("datetime")
            .HasColumnName("create_date");
        builder.Property(e => e.ModifiedDate)
            .HasColumnType("datetime")
            .HasColumnName("modified_date");
        builder.Property(e => e.CreateBy)
            .HasMaxLength(50)
            .HasColumnName("create_by");
        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasColumnName("modified_by");
        #endregion
    }
}