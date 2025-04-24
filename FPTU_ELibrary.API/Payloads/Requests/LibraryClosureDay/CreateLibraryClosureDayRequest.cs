namespace FPTU_ELibrary.API.Payloads.Requests.LibraryClosureDay;

public class CreateLibraryClosureDayRequest
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int? Year { get; set; }
    public string? VieDescription { get; set; }
    public string? EngDescription { get; set; }
}