using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTrackingDetail;

public class ImportTrackingDetailRequest
{
    public IFormFile File { get; set; } = null!;
    public string[] ScanningFields { get; set; } = [];
    public DuplicateHandle? DuplicateHandle { get; set; }
}