namespace FPTU_ELibrary.API.Payloads.Requests.OCR;

public class CheckImagesForTrainingRequest  
{
    public int ItemId { get; set;}
    public List<IFormFile> CompareList { get; set; }
}