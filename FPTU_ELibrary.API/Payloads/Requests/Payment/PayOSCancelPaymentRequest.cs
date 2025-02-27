namespace FPTU_ELibrary.API.Payloads.Requests.Payment;

public class PayOSCancelPaymentRequest
{
    public string OrderCode { get; set; } = null!;
    public string CancellationReason { get; set; } = null!;
}