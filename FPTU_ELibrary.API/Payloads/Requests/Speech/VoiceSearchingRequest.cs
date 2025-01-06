namespace FPTU_ELibrary.API.Payloads.Requests.Speech;

public class VoiceSearchingRequest
{
    public string LanguageCode { get; set; }
    public IFormFile AudioFile { get; set; }
}