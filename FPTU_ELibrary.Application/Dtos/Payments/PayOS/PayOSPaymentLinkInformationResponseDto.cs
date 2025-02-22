namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentLinkInformationResponseDto
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentLinkInformationDataDto Data { get; set; } = null!;
    public string Signature { get; set; } = string.Empty;
    public bool Success { get; set; }
}