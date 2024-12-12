namespace FPTU_ELibrary.API.Payloads.Requests.Resource;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
}