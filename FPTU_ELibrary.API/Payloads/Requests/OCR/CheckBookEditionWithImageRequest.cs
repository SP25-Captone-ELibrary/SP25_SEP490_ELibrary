namespace FPTU_ELibrary.API.Payloads.Requests;

public class CheckBookEditionWithImageRequest
{
    public string Title { get; set; }
    public string Publisher { get; set; }
    public List<string> Authors { get; set; }
    public IFormFile Image { get; set;}
}