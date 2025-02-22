namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class AddLibraryCardAsync : RegisterLibraryCardOnlineRequest
{
    public Guid UserId { get; set; }
    
    // Library card package (use when create with cash payment)
    public int? LibraryCardPackageId { get; set; }
    public int? PaymentMethodId { get; set; }
}