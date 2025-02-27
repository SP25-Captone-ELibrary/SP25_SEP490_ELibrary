using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class CreateLibraryCardHolderRequest
{
    // User information
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Gender { get; set; } = null!;
    public DateTime? Dob { get; set; }
    
    // Library card information
    public string Avatar { get; set; } = null!;
    
    // Payment information
    public TransactionMethod TransactionMethod { get; set; }
    public int? PaymentMethodId { get; set; } // This required when transaction method is DigitalPayment
    
    // Library card package
    public int LibraryCardPackageId { get; set; }
}