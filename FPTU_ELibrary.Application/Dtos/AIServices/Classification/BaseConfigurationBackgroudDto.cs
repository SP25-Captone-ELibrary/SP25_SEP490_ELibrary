using FPTU_ELibrary.Application.Configurations;
using Serilog;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class BaseConfigurationBackgroudDto
{
    public HttpClient Client { get; set; }
    public CustomVisionSettings Configuration { get; set; }
    public string BaseUrl { get; set; }
    public ILogger Logger { get; set; }
}