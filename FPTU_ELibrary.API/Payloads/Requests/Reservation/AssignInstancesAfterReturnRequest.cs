namespace FPTU_ELibrary.API.Payloads.Requests.Reservation;

public class AssignInstancesAfterReturnRequest
{
    public List<int> LibraryItemInstanceIds { get; set; } = new();
}