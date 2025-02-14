namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class AddLibraryCardAsync : RegisterLibraryCardOnlineRequest
{
    public Guid UserId { get; set; }
}