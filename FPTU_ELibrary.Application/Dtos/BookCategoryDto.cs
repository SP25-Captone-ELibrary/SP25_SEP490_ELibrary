namespace FPTU_ELibrary.Application.Dtos;

public class BookCategoryDto
{
    // Key
    public int CategoryId { get; set; }
    // Category detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
    public bool? IsDelete { get; set; }
    public ICollection<BookDto> BookDtos { get; set; } = new List<BookDto>();

}