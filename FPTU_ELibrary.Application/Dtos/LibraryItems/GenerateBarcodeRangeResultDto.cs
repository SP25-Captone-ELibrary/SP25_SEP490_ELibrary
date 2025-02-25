namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class GenerateBarcodeRangeResultDto
{
    public string BarcodeRangeFrom { get; set; } = null!;
    public string BarcodeRangeTo { get; set; } = null!;
    public List<string> Barcodes { get; set; } = new();
}