namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentResponseDto
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentDataDto Data { get; set; } = null!;
    public string Signature { get; set; } = string.Empty;
}