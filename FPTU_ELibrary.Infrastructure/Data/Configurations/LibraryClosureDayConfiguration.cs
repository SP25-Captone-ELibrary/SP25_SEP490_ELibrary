using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations;

public class LibraryClosureDayConfiguration : IEntityTypeConfiguration<LibraryClosureDay>
{
    public void Configure(EntityTypeBuilder<LibraryClosureDay> builder)
    {
        #region Added at 24/04/2025 by Le Xuan Phuoc
        builder.HasKey(e => e.ClosureDayId).HasName("PK_LibraryClosureDay");

        builder.ToTable("Library_Closure_Day");

        builder.Property(e => e.ClosureDayId).HasColumnName("closure_day_id");
        builder.Property(e => e.Day).HasColumnName("day");
        builder.Property(e => e.Month).HasColumnName("month");
        builder.Property(e => e.Year).HasColumnName("year");
        builder.Property(e => e.VieDescription)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("vie_description");
        builder.Property(e => e.EngDescription)
            .HasColumnType("nvarchar(255)")
            .HasColumnName("eng_description");
        #endregion
    }
}