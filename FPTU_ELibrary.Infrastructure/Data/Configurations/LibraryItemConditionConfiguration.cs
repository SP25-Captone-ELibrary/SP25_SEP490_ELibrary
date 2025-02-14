using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryItemConditionConfiguration : IEntityTypeConfiguration<LibraryItemCondition>
{
    public void Configure(EntityTypeBuilder<LibraryItemCondition> builder)
    {
        #region Added at 12/02/2025
        builder.HasKey(e => e.ConditionId).HasName("PK_LibraryItemCondition_ConditionId");
        
        builder.ToTable("Library_Item_Condition");
        
        builder.Property(e => e.ConditionId).HasColumnName("ConditionId");
        #endregion

        #region Update at 13/02/2025
        // builder.Property(e => e.Description)
        //     .HasColumnType("nvarchar(100)")
        //     .HasColumnName("description");
        
        builder.Property(e => e.EnglishName)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("english_name");
        builder.Property(e => e.VietnameseName)
            .HasColumnType("nvarchar(50)")
            .HasColumnName("vietnamese_name");
        #endregion
    }
}