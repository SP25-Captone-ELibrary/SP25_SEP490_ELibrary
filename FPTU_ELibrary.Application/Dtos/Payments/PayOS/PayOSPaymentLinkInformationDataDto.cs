namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentLinkInformationDataDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedAt { get; set; }
    public List<PayOSTransactionDto> Transactions { get; set; } = new();
    public string? CancellationReason { get; set; } = string.Empty;
    public string? CanceledAt { get; set; } 
}

public class PayOSTransactionDto
{
    public string? Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string? VirtualAccountName { get; set; } = string.Empty;
    public string? VirtualAccountNumber { get; set; } = string.Empty;
    public string? CounterAccountBankId { get; set; } = string.Empty;
    public string? CounterAccountBankName { get; set; } = string.Empty;
    public string? CounterAccountName { get; set; } = string.Empty;
    public string? CounterAccountNumber { get; set; } = string.Empty;
}
