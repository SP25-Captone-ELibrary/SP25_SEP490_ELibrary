namespace FPTU_ELibrary.API.Payloads.Requests.FaceDetection;

public class PersonFaceDetectionRequest
{
    public IFormFile File { get; set; } = null!;
    public string[] Attributes { get; set; } = [];
}