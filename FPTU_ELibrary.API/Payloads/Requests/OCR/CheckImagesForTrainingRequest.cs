namespace FPTU_ELibrary.API.Payloads.Requests;

public class CheckImagesForTrainingRequest
{
    public List<int> ItemIds { get; set;}
    public List<IFormFile> CompareList { get; set; }
}