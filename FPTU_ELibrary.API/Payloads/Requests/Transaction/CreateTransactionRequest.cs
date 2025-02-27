using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Transaction;

// This request only process by user, hence requiring payment method go along with
public class CreateTransactionRequest
{
    // Transaction for 
    public int? ResourceId { get; set; }
    public int? LibraryCardPackageId { get; set; }
    
    // Transaction description
    public string? Description { get; set; }
    
    // Transaction method
    public int PaymentMethodId { get; set; }
    
    // Transaction type
    public TransactionType TransactionType { get; set; }
}