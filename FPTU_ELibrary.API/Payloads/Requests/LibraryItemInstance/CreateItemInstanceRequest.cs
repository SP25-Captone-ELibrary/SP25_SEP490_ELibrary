namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

public class CreateItemInstanceRequest
{
    public string Barcode { get; set; } = null!;
    // Good, Worn, Damaged
    public string ConditionStatus { get; set; } = null!;
}