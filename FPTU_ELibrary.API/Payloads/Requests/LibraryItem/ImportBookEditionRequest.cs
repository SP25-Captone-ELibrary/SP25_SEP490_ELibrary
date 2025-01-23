using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class ImportBookEditionRequest
{
    public IFormFile? File { get; set; } = null!;
    public List<IFormFile> CoverImageFiles { get; set; } = new();
    public string[]? ScanningFields { get; set; }
    public DuplicateHandle? DuplicateHandle { get; set; } 
}