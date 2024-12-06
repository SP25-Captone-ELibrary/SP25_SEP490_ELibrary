namespace FPTU_ELibrary.API.Payloads.Requests.SystemMessage;

public class ImportMessageRequest
{
    public IFormFile File { get; set; } = null!;
}