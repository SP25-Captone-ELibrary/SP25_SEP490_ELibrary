using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryPathConfiguration : IEntityTypeConfiguration<LibraryPath>
    {
        public void Configure(EntityTypeBuilder<LibraryPath> builder)
        {
            builder.HasKey(e => e.PathId).HasName("PK_LibraryPath_PathId");

            builder.ToTable("Library_Path");

            builder.Property(e => e.PathId).HasColumnName("path_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.Distance).HasColumnName("distance");
            builder.Property(e => e.FromZoneId).HasColumnName("from_zone_id");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.PathDescription)
                .HasMaxLength(255)
                .HasColumnName("path_description");
            builder.Property(e => e.ToZoneId).HasColumnName("to_zone_id");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");

            builder.HasOne(d => d.FromZone).WithMany(p => p.LibraryPathFromZones)
                .HasForeignKey(d => d.FromZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryPath_FromZoneId");

            builder.HasOne(d => d.ToZone).WithMany(p => p.LibraryPathToZones)
                .HasForeignKey(d => d.ToZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LibraryPath_ToZoneId");
        }
    }
}
