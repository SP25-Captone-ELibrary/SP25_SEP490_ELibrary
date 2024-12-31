namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateRangeBookEditionCopyRequest
{
    public List<string> Codes { get; set; } = new();
}