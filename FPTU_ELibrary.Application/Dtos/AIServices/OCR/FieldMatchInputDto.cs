using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class FieldMatchInputDto
{
    public string FieldName { get; set; }
    public List<string> Values { get; set; }
    public double Weight { get; set; }
}