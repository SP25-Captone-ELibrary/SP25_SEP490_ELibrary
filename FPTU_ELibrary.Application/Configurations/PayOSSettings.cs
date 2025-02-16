namespace FPTU_ELibrary.Application.Configurations;

public class PayOSSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    // public string WebHookUrl { get; set; } = string.Empty;
    public string ConfirmWebHookUrl { get; set; } = string.Empty;
    public string GetPaymentLinkInformationUrl { get; set; } = string.Empty;
    public string CancelPaymentUrl { get; set; } = string.Empty;
}