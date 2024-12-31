using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class CheckedBookEditionDto
{
    public string Title { get; set; }
    public string Publisher { get; set; }
    public List<string> Authors { get; set; }
    public IFormFile Image { get; set;}
}