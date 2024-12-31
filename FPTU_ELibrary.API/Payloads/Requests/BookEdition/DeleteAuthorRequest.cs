namespace FPTU_ELibrary.API.Payloads.Requests.BookEdition;

public class DeleteAuthorRequest
{
    public int BookEditionId { get; set; }
    public int AuthorId { get; set; }
}