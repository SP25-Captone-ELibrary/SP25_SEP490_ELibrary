using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryZoneConfiguration : IEntityTypeConfiguration<LibraryZone>
    {
        public void Configure(EntityTypeBuilder<LibraryZone> builder)
        {
            builder.HasKey(e => e.ZoneId).HasName("PK_LibraryZone_ZoneId");

            builder.ToTable("Library_Zone");

            builder.Property(e => e.ZoneId).HasColumnName("zone_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.FloorId).HasColumnName("floor_id");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.Floor).WithMany(p => p.LibraryZones)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryZone_FloorId");

            #region Update at 28/02/2025 by Le Xuan Phuoc
            // builder.Property(e => e.XCoordinate).HasColumnName("x_coordinate");
            // builder.Property(e => e.YCoordinate).HasColumnName("y_coordinate");
            
            builder.Property(e => e.EngZoneName)
                .HasMaxLength(100)
                .HasColumnName("eng_zone_name");
            
            builder.Property(e => e.VieZoneName)
                .HasMaxLength(100)
                .HasColumnName("vie_zone_name");
            
            builder.Property(e => e.TotalCount)
                .HasDefaultValue(0)
                .HasColumnName("total_count");
            
            builder.Property(e => e.EngDescription)
                .HasMaxLength(255)
                .HasColumnName("eng_description");
            builder.Property(e => e.VieDescription)
                .HasMaxLength(255)
                .HasColumnName("vie_description");
            #endregion
        }
    }
}
