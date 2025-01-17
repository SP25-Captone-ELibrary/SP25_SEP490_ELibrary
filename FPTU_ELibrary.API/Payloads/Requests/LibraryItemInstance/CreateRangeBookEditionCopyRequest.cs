namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateRangeBookEditionCopyRequest
{
    public List<CreateLibraryItemInstanceRequest> BookEditionCopies { get; set; } = new();
}