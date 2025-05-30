﻿using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class FineConfiguration : IEntityTypeConfiguration<Fine>
    {
        public void Configure(EntityTypeBuilder<Fine> builder)
        {
            builder.HasKey(e => e.FineId).HasName("PK_Fine_FineId");

            builder.ToTable("Fine");

            builder.Property(e => e.FineId).HasColumnName("fine_id");
            builder.Property(e => e.FineNote)
                .HasMaxLength(255)
                .HasColumnName("fine_note");
            builder.Property(e => e.FinePolicyId).HasColumnName("fine_policy_id");
            builder.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            builder.Property(e => e.BorrowRecordDetailId).HasColumnName("borrow_record_detail_id");
            builder.HasOne(d => d.BorrowRecordDetail).WithMany(p => p.Fines)
                .HasForeignKey(d => d.BorrowRecordDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fine_BorrowRecordDetailId");

            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.HasOne(d => d.CreateByNavigation).WithMany(p => p.FineCreateByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fine_CreatedBy");

            builder.HasOne(d => d.FinePolicy).WithMany(p => p.Fines)
                .HasForeignKey(d => d.FinePolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fine_FindPolicyId");

            #region Update at 16/01/2025 by Le Xuan Phuoc
            // builder.Property(e => e.Amount)
            //         .HasColumnType("decimal(10, 2)")
            //         .HasColumnName("amount");
            // builder.Property(e => e.CompensateBy).HasColumnName("compensate_by");
            // builder.Property(e => e.CompensateType)
            //     .HasMaxLength(50)
            //     .HasColumnName("compensate_type");
            // builder.Property(e => e.CompensationDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("compensation_date");
            // builder.Property(e => e.CompensationNote)
            //     .HasMaxLength(255)
            //     .HasColumnName("compensation_note");
            // builder.Property(e => e.CompensationStatus)
            //     .HasMaxLength(50)
            //     .HasColumnName("compensation_status");
            // builder.Property(e => e.CreateBy).HasColumnName("create_by");
            // builder.Property(e => e.CreateDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("create_date");    
            // builder.Property(e => e.PaymentDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("payment_date");

            #endregion

            #region Updated at 14/03/2025 by Le Xuan Phuoc
            builder.Property(e => e.ExpiryAt)
                .IsRequired(false)
                .HasColumnType("datetime")
                .HasColumnName("expiry_at");
            builder.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("status");
            builder.Property(e => e.FineAmount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("fine_amount");
            #endregion

            #region Updated at 25/04/2025 by Le Xuan Phuoc
            builder.Property(e => e.DamagePct)
                .HasColumnType("decimal(3,2)")
                .HasColumnName("damage_pct");
            #endregion
        }
    }
}
