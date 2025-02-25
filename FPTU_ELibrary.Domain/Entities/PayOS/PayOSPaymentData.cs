namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentData
{
    public string Bin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; } 
    public string Description { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string Curency { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}