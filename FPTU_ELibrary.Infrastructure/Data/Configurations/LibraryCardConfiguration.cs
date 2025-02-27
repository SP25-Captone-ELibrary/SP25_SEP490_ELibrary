using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Hosting;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryCardConfiguration : IEntityTypeConfiguration<LibraryCard>
{
    public void Configure(EntityTypeBuilder<LibraryCard> builder)
    {
        #region Added at 13/01/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.LibraryCardId).HasName("PK_LibraryCard_LibraryCardId");
        
        builder.ToTable("Library_Card");

        builder.Property(e => e.LibraryCardId)
            .HasDefaultValueSql("(newsequentialid())")
            .HasColumnName("library_card_id");
        builder.Property(e => e.FullName)
            .HasColumnType("nvarchar(200)")
            .HasColumnName("full_name");
        builder.Property(e => e.Avatar)
            .HasMaxLength(2048)
            .IsUnicode(false)
            .HasColumnName("avatar");
        builder.Property(e => e.Barcode)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("barcode");
        builder.Property(e => e.IssuanceMethod)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("issuance_method");
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)")
            .HasColumnName("status");
        builder.Property(e => e.IssueDate)
            .HasColumnType("datetime")
            .HasColumnName("issue_date");
        builder.Property(e => e.ExpiryDate)
            .HasColumnType("datetime")
            .HasColumnName("expiry_date");
        builder.Property(e => e.IsExtended)
            .HasDefaultValue(false)
            .HasColumnName("is_extended");
        builder.Property(e => e.ExtensionCount)
            .HasDefaultValue(0)
            .HasColumnName("extension_count");
        #endregion

        #region Update at 04/02/2025 by Le Xuan Phuoc
        // builder.Property(e => e.IsActive).HasColumnName("is_active");
        
        builder.Property(e => e.SuspensionEndDate)
            .HasColumnType("datetime")
            .HasColumnName("suspension_end_date");
        #endregion

        #region Update at 06/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.IsReminderSent)
            .HasDefaultValue(false)
            .HasColumnName("is_reminder_sent");
        builder.Property(e => e.IsAllowBorrowMore)
            .HasDefaultValue(false)
            .HasColumnName("is_allow_borrow_more");
        builder.Property(e => e.MaxItemOnceTime)
            .HasDefaultValue(0)
            .HasColumnName("max_item_once_time");
        builder.Property(e => e.TotalMissedPickUp)
            .HasDefaultValue(0)
            .HasColumnName("total_missed_pick_up");
        #endregion

        #region Update at 11/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.IsArchived)
            .HasDefaultValue(false)
            .HasColumnName("is_archived");
        builder.Property(e => e.ArchiveReason)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("archive_reason");
        builder.Property(e => e.PreviousUserId)
            .HasColumnName("previous_user_id");
        #endregion

        #region Update at 14/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.AllowBorrowMoreReason)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("allow_borrow_more_reason");
        
        builder.Property(e => e.SuspensionReason)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("suspension_reason");
        #endregion

        #region Update at 17/02/2025 by Le Xuan Phuoc
        builder.Property(e => e.TransactionCode)
            .IsRequired(false)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("transaction_code");
        builder.Property(e => e.RejectReason)
            .IsRequired(false)
            .HasColumnType("nvarchar(250)")
            .HasColumnName("reject_reason");
        #endregion
    }
}