namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class GetGroupableLibraryItemRequest
{
    public string Title { get; set; } = null!;
    public string CutterNumber { get; set; } = null!;
    public string ClassificationNumber { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
}