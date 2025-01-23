namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

public class UpdateItemInstanceRequest
{
    public string Status { get; set; } = null!;
    public string Barcode { get; set; } = null!;
}