namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class UserExtendLibraryCardRequest
{
    // This used to validate whether register request from a continuous progress
    public string? TransactionToken { get; set; } = null!;
}