namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class ArchiveLibraryCardRequest
{
    public Guid LibraryCardId { get; set; }
    public string ArchiveReason { get; set; } = null!;
}