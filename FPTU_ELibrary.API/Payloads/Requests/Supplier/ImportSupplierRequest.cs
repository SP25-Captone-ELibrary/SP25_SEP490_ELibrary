using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Supplier;

public class ImportSupplierRequest
{
    public IFormFile File { get; set; } = null!;
    public DuplicateHandle? DuplicateHandle { get; set; }
    public string[] ScanningFields { get; set; } = [];
}