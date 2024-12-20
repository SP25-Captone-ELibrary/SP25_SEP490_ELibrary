using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class CategoryDto
{
    // Key
    public int CategoryId { get; set; }

    // Category detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<BookCategoryDto> BookCategories { get; set; } = new List<BookCategoryDto>();
}