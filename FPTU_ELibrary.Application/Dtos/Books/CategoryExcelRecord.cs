using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class CategoryExcelRecord
{
    public string? EnglishName { get; set; } = null!;
    public string? VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
}

public class BookCategoryFailedMessage
{
    public int Row { get; set; }
    public List<string> ErrMsg { get; set; } = null!;
}

public static class BookCategoryExcelRecordExtension
{
    public static CategoryDto ToBookCategoryDto(this CategoryExcelRecord req)
    {
        return new CategoryDto()
        {
            EnglishName = req.EnglishName,
            VietnameseName = req.VietnameseName,
            Description = req.Description
        };
    }
    
    public static List<CategoryExcelRecord> ToBookCategoryExcelRecords(this IEnumerable<CategoryDto> bookCategories)
    {
        return bookCategories.Select(e =>
        {
            return new CategoryExcelRecord()
            {
                EnglishName = e.EnglishName,
                VietnameseName = e.VietnameseName,
                Description = e.Description
            };
        }).ToList();
    }
}