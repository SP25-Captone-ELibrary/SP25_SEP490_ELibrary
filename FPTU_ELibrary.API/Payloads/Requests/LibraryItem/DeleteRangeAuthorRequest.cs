namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class DeleteRangeAuthorRequest
{
    public int LibraryItemId { get; set; }
    public int[] AuthorIds { get; set; } = null!;
}