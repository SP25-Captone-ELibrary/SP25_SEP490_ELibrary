using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class CheckedItemDto
{
    public string Title { get; set; } = null!; // marc21 code 245a
    public string? SubTitle { get; set; } // marc21 code 245b
    public string? GeneralNote { get; set; } // marc21 code 500
    public string Publisher { get; set; } = null!; // marc21 code 260
    public List<string> Authors { get; set; } = new(); // marc21 code[700a]/[700a + 700e]
    public List<IFormFile> Images { get; set; } = null!;
}