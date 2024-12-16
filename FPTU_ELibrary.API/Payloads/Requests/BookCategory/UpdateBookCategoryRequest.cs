namespace FPTU_ELibrary.API.Payloads.Requests;

public class UpdateBookCategoryRequest
{
    public string? EnglishName { get; set; } = null!;
    public string? VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
}