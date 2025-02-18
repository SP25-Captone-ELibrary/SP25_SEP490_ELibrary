namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class RegisterLibraryCardOnlineRequest
{
    // Library card information
    public string Avatar { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? TransactionToken { get; set; } = null!;
}