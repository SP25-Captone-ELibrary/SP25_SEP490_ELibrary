namespace FPTU_ELibrary.API.Payloads.Requests.OCR;

public class CheckImagesForTrainingRequest
{
    public List<int> ItemIds { get; set; } = new();
    public List<IFormFile> CompareList { get; set; } = new();
}