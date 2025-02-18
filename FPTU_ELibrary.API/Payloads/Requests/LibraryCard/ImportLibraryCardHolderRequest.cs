using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class ImportLibraryCardHolderRequest
{
    public IFormFile? File { get; set; } = null!;
    public List<IFormFile>? AvatarImageFiles { get; set; }
    public string[]? ScanningFields { get; set; }
    public DuplicateHandle? DuplicateHandle { get; set; } 
}