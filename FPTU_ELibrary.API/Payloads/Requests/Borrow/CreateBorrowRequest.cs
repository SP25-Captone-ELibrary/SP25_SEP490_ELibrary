namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class CreateBorrowRequest
{
    public string? Description { get; set; }
    public List<int> LibraryItemIds { get; set; } = new();
    public List<int> ReservationItemIds { get; set; } = new();
    public List<int> ResourceIds { get; set; } = new();
}