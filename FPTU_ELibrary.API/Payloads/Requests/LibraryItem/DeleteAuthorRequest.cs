namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class DeleteAuthorRequest
{
    public int LibraryItemId { get; set; }
    public int AuthorId { get; set; }
}