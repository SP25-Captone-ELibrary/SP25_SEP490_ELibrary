namespace FPTU_ELibrary.API.Payloads.Requests.Payment;

public class PayOSCancelPaymentRequest
{
    public string CancellationReason { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
}