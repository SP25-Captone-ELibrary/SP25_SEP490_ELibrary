using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.API.Payloads.Requests;

public class CreateBookCategoryRequest
{
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
}

public static class CreateBookCategoryRequestExtension
{
    public static BookCategoryDto ToBookCategoryDto(this CreateBookCategoryRequest req)
    {
        return new BookCategoryDto()
        {
            Description = req.Description,
            EnglishName = req.EnglishName,
            VietnameseName = req.VietnameseName
        };
    }
}