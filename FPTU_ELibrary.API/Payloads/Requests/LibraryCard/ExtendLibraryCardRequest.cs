using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class ExtendLibraryCardRequest 
{
    // Payment information
    public TransactionMethod TransactionMethod { get; set; }
    public int? PaymentMethodId { get; set; } // This required when transaction method is DigitalPayment
    
    // Library card package
    public int LibraryCardPackageId { get; set; }
}