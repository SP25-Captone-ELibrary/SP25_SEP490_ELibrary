
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class GetTopBorrowResourceDto
{
    public LibraryResourceDto LibraryResource { get; set; } = null!;
    public int TotalBorrowed { get; set; }
    public int TotalExtension { get; set; }
    public double AverageBorrowDuration { get; set; }
    public double ExtensionRate { get; set; }
    public DateTime? LastBorrowDate { get; set; }
}