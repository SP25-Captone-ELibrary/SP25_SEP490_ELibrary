using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class AddLibraryCardRequest : RegisterLibraryCardOnlineRequest
{
    public Guid UserId { get; set; }

    // Payment information
    public TransactionMethod TransactionMethod { get; set; }
    public int? PaymentMethodId { get; set; } // This required when transaction method is DigitalPayment
    
    // Library card package
    public int LibraryCardPackageId { get; set; }
}