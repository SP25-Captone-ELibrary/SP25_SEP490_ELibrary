namespace FPTU_ELibrary.API.Payloads.Requests.Category;

public class UpdateCategoryRequest
{
    public string Prefix { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsAllowAITraining { get; set; }
    public int TotalBorrowDays { get; set; }
}