namespace FPTU_ELibrary.API.Payloads.Requests.Reservation;

public class GetAllAssignableAfterReturnRequest
{
    public List<int> LibraryItemInstanceIds { get; set; } = new();
}