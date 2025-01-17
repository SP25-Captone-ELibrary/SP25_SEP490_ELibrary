namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class UpdateRangeBookEditionCopyRequest
{
    public List<int> BookEditionCopyIds { get; set; } = new();
    public string Status { get; set; } = null!;
}