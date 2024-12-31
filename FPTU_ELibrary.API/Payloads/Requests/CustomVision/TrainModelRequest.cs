namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class TrainModelRequest
{
    public int BookId { get; set; }
    public List<IFormFile> ImageList { get; set; }
}