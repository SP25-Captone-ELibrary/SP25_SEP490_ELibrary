namespace FPTU_ELibrary.Application.Dtos.AudioCloud;

public class GenerateMultipartUploadUrl
{
    public List<string> Urls { get; set; } = [];
    public string S3PathKey { get; set; } = null!;
    public string UploadId { get; set; } = null!;
}