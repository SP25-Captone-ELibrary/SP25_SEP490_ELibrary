namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class GetBarcodeRegistrationResultDto
{
    public WarehouseTrackingDetailDto WarehouseTrackingDetail { get; set; } = null!;
    public List<string> Barcodes { get; set; } = new();
}