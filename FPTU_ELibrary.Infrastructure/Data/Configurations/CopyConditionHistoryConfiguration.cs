using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class CopyConditionHistoryConfiguration : IEntityTypeConfiguration<CopyConditionHistory>
    {
        public void Configure(EntityTypeBuilder<CopyConditionHistory> builder)
        {
            builder.HasKey(e => e.ConditionHistoryId).HasName("PK_Book_Condition_History");

            builder.ToTable("Copy_Condition_History");

            builder.Property(e => e.ConditionHistoryId).HasColumnName("condition_history_id");
            builder.Property(e => e.BookEditionCopyId).HasColumnName("book_edition_copy_id");
            builder.Property(e => e.Condition)
                .HasMaxLength(50)
                .HasColumnName("condition");

            builder.HasOne(d => d.BookEditionCopy).WithMany(p => p.CopyConditionHistories)
                .HasForeignKey(d => d.BookEditionCopyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConditionHistory_BookEditionCopyId");

            #region Update at 24/12/2024 by Le Xuan Phuoc
            // builder.Property(e => e.ChangeDate)
            //     .HasColumnType("datetime")
            //     .HasColumnName("change_date");
            // builder.Property(e => e.ChangedBy).HasColumnName("changed_by");
            // builder.Property(e => e.Condition)
            //     .HasMaxLength(50)
            //     .HasColumnName("condition");
            // builder.HasOne(d => d.ChangedByNavigation).WithMany(p => p.CopyConditionHistories)
            //     .HasForeignKey(d => d.ChangedBy)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_ConditionHistory_ChangedBy");
            
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
