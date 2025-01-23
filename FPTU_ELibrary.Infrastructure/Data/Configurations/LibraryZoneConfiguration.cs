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
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");
            builder.Property(e => e.XCoordinate).HasColumnName("x_coordinate");
            builder.Property(e => e.YCoordinate).HasColumnName("y_coordinate");
            builder.Property(e => e.ZoneName)
                .HasMaxLength(100)
                .HasColumnName("zone_name");

            builder.HasOne(d => d.Floor).WithMany(p => p.LibraryZones)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryZone_FloorId");
        }
    }
}
