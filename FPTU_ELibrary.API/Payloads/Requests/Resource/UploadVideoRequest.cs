namespace FPTU_ELibrary.API.Payloads.Requests.Resource;

public class UploadVideoRequest
{
    public IFormFile File { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
}