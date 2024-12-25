using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.API.Payloads.Requests;

public class CreateCategoryRequest
{
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
}

public static class CreateBookCategoryRequestExtension
{
    public static CategoryDto ToCategoryDto(this CreateCategoryRequest req)
    {
        return new CategoryDto()
        {
            Description = req.Description,
            EnglishName = req.EnglishName,
            VietnameseName = req.VietnameseName
        };
    }
}