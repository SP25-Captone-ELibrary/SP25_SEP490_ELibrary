namespace FPTU_ELibrary.Application.Dtos.Fine;

public class BookCategoryExcelRecord
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
    public static BookCategoryDto ToBookCategoryDto(this BookCategoryExcelRecord req)
    {
        return new BookCategoryDto()
        {
            EnglishName = req.EnglishName,
            VietnameseName = req.VietnameseName,
            Description = req.Description
        };
    }
    
    public static List<BookCategoryExcelRecord> ToBookCategoryExcelRecords(this IEnumerable<BookCategoryDto> bookCategories)
    {
        return bookCategories.Select(e =>
        {
            return new BookCategoryExcelRecord()
            {
                EnglishName = e.EnglishName,
                VietnameseName = e.VietnameseName,
                Description = e.Description
            };
        }).ToList();
    }
}