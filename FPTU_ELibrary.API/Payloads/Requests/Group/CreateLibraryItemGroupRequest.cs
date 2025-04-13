namespace FPTU_ELibrary.API.Payloads.Requests.Group;

public class CreateLibraryItemGroupRequest
{
    public string ClassificationNumber { get; set; } = null!;
    public string CutterNumber { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string Author { get; set; } = null!;
    public string? TopicalTerms { get; set; }
}