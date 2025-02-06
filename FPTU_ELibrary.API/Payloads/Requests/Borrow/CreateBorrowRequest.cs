namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class CreateBorrowRequest
{
    public string? Description { get; set; }
    public List<int> LibraryItemIds { get; set; } = new();
}