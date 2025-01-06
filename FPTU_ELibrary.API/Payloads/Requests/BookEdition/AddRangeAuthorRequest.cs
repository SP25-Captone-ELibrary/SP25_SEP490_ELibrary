namespace FPTU_ELibrary.API.Payloads.Requests.BookEdition;

public class AddRangeAuthorRequest
{
    public int BookEditionId { get; set; }
    public int[] AuthorIds { get; set; } = null!;
}