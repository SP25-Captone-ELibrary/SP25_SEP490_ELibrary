using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class LibraryFloorConfiguration : IEntityTypeConfiguration<LibraryFloor>
    {
        public void Configure(EntityTypeBuilder<LibraryFloor> builder)
        {
            builder.HasKey(e => e.FloorId).HasName("PK_LibraryFloor_FloorId");

            builder.ToTable("Library_Floor");

            builder.Property(e => e.FloorId).HasColumnName("floor_id");
            builder.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("create_date");
            builder.Property(e => e.FloorNumber)
                .HasMaxLength(50)
                .HasColumnName("floor_number");
            builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            builder.Property(e => e.UpdateDate)
                .HasColumnType("datetime")
                .HasColumnName("update_date");
        }
    }
}
