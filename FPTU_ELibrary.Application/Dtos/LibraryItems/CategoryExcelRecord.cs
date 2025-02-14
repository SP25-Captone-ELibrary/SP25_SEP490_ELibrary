using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class CategoryExcelRecord
{
    public string Prefix { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsAllowAITraining { get; set; }
    public int TotalBorrowDays { get; set; }
}

public class BookCategoryFailedMessage
{
    public int Row { get; set; }
    public List<string> ErrMsg { get; set; } = null!;
}
