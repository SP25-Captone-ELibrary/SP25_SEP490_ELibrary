namespace FPTU_ELibrary.API.Payloads.Requests.Resource;

public class UploadSmallAudioRequest
{
    public IFormFile File { get; set; } = null!;
}