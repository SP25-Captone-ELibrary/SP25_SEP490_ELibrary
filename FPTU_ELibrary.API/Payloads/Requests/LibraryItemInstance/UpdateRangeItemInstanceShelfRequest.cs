namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

public class UpdateRangeItemInstanceShelfRequest
{
    public List<string> Barcodes { get; set; } = new();
}