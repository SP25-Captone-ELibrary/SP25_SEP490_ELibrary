using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Locations;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class BookDetailDto
{
    // Book information
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Summary { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsDraft { get; set; }
    
    // Book editions
    public List<BookEditionDetailDto> BookEditions { get; set; } = new();
    
    // Categories
    public List<CategoryDto> Categories { get; set; } = new();
    // Resources
    public List<BookResourceDto> BookResources { get; set; } = new();
}

public static class BookDetailDtoExtensions
{
    public static BookDetailDto ToBookDetailDto(this BookDto dto)
    {
        return new BookDetailDto()
        {
            // Book information
            BookId = dto.BookId,
            Title = dto.Title,
            SubTitle = dto.SubTitle,
            Summary = dto.Summary,
            IsDeleted = dto.IsDeleted,
            IsDraft = dto.IsDraft,
            
            // Book Editions
            BookEditions = dto.BookEditions.Select(be => 
                be.ToEditionDetailDtoWithBookDetail(dto.Title, dto.SubTitle, dto.Summary)).ToList(),
            
            // Categories
            Categories = dto.BookCategories.Select(bc => bc.Category).ToList(),
            // Book resources
            BookResources = dto.BookResources.ToList()
        };
    }
}