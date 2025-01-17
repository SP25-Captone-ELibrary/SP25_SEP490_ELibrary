namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateLibraryItemInstanceRequest
{
    public string Barcode { get; set; } = null!;
    // Good, Worn, Damaged
    public string ConditionStatus { get; set; } = null!;
}