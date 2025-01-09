using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class CheckedBookEditionDto
{
    public string Title { get; set; } = null!;
    public string Publisher { get; set; } = null!;
    public List<string> Authors { get; set; } = new();
    public IFormFile Image { get; set; } = null!;
}