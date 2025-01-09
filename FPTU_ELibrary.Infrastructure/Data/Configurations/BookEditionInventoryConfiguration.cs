using FPTU_ELibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTU_ELibrary.Infrastructure.Data.Configurations
{
    public class BookEditionInventoryConfiguration : IEntityTypeConfiguration<BookEditionInventory>
    {
        public void Configure(EntityTypeBuilder<BookEditionInventory> builder)
        {
            builder.HasKey(e => e.BookEditionId).HasName("PK_BookEditionInventory_BookEditionId");

            builder.ToTable("Book_Edition_Inventory");

            builder.Property(e => e.BookEditionId)
                .ValueGeneratedNever()
                .HasColumnName("book_edition_id");
            builder.Property(e => e.AvailableCopies).HasColumnName("available_copies");
            builder.Property(e => e.RequestCopies).HasColumnName("request_copies");
            builder.Property(e => e.BorrowedCopies).HasColumnName("borrowed_copies");
            builder.Property(e => e.ReservedCopies).HasColumnName("reserved_copies");
            builder.Property(e => e.TotalCopies).HasColumnName("total_copies");

            builder.HasOne(d => d.BookEdition).WithOne(p => p.BookEditionInventory)
                .HasForeignKey<BookEditionInventory>(d => d.BookEditionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_BookEditionInventory_BookEditionId");
        }
    }
}
