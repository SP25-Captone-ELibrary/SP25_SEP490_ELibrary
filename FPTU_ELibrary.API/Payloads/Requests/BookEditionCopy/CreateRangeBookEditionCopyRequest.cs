namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateRangeBookEditionCopyRequest
{
    public List<CreateBookEditionCopyRequest> BookEditionCopies { get; set; } = new();
}