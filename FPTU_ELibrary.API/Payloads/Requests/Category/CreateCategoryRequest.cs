using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.API.Payloads.Requests;

public class CreateCategoryRequest
{
    public string Prefix { get; set; } = null!;
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
            Prefix = req.Prefix,
            Description = req.Description,
            EnglishName = req.EnglishName,
            VietnameseName = req.VietnameseName
        };
    }
}