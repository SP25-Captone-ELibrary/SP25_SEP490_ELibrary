
namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentRequest
{
    public int OrderCode { get; set; } 
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public List<object> Items { get; set; } = new();
    public string CancelUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public int ExpiredAt { get; set; }
    public string Signature { get; set; } = string.Empty;
}