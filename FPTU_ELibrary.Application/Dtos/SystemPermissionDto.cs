namespace FPTU_ELibrary.Application.Dtos;

public class SystemPermissionDto
{
    public int PermissionId { get; set; }
    public int PermissionLevel { get; set; }
    public string VietnameseName { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
}