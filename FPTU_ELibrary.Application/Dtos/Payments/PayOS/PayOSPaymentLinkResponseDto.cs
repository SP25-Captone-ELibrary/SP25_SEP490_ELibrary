namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentLinkResponseDto
{
    public PayOSPaymentResponseDto PayOsResponse { get; set; } = null!;
    public int ExpiredAtOffsetUnixSeconds { get; set; }
}