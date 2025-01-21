namespace FPTU_ELibrary.API.Payloads.Requests.OCR;

public class CheckImagesForTrainingRequest
{
    public int BookEditionId { get; set; }
    public List<IFormFile> Images { get; set; }
}