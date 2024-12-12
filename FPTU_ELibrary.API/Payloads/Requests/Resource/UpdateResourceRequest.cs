namespace FPTU_ELibrary.API.Payloads.Requests.Resource;

public class UpdateResourceRequest
{
    public IFormFile File { get; set; } = null!;
    public string PublicId { get; set; } = null!;
}