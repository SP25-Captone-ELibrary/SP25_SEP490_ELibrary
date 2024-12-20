namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BookCategorySpecParams : BaseSpecParams
{
    public string? EnglishName { get; set; } = null!;
    public string? VietnameseName { get; set; } = null!;
}