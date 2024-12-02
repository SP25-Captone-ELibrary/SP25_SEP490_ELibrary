namespace FPTU_ELibrary.Domain.Entities.Base;

public class BaseRole : IBaseRole
{
    public string VietnameseName { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
}