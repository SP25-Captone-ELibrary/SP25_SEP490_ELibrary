namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class ExtendLibraryCardRequest : UserExtendLibraryCardRequest
{
    // Library card package (use when create with cash payment)
    public int? LibraryCardPackageId { get; set; }
    public int? PaymentMethodId { get; set; }
}