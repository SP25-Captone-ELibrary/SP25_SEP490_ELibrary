namespace FPTU_ELibrary.API.Payloads.Requests.Payment;

public class SendWebhookConfirmRequest
{
    public string WebhookUrl { get; set; } = null!;
}