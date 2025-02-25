namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentResponse
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentData Data { get; set; } = null!;
    public string Signature { get; set; } = string.Empty;
}