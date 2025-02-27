using FPTU_ELibrary.Application.Utils;

namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentLinkInformationResponseDto
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentLinkInformationDataDto Data { get; set; } = null!;
    public string Signature { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public static class PayOSPaymentLinkInformationResponseDtoExtensions
{
    /// <summary>
    /// Generate webhook signature
    /// </summary>
    /// <param name="resp"></param>
    /// <param name="paymentLinkId"></param>
    /// <param name="checksumKey"></param>
    /// <returns></returns>
    public static async Task<string> GenerateWebhookSignatureAsync(this PayOSPaymentLinkInformationResponseDto resp,
        string paymentLinkId, string checksumKey)
    {
        var rawSignature =
            $"orderCode={resp.Data.OrderCode}&amount={resp.Data.Amount}&description={resp.Data.Transactions[0].Description}" +
            $"&accountNumber={resp.Data.Transactions[0].AccountNumber}&reference={resp.Data.Transactions[0].Reference}&transactionDateTime={resp.Data.Transactions[0].TransactionDateTime}" +
            $"&currency=VND&paymentLinkId={paymentLinkId}&code={resp.Code}&desc={resp.Desc}" +
            $"&counterAccountBankId={resp.Data.Transactions[0].CounterAccountBankId}&counterAccountBankName={resp.Data.Transactions[0].CounterAccountBankName}" +
            $"&counterAccountName={resp.Data.Transactions[0].CounterAccountName}&counterAccountNumber={resp.Data.Transactions[0].CounterAccountNumber}" +
            $"&virtualAccountName={resp.Data.Transactions[0].VirtualAccountName}&virtualAccountNumber={resp.Data.Transactions[0].VirtualAccountNumber}";
        // Split the raw signature string into key-value pairs
        List<string> keyValuePairs = rawSignature.Split('&').ToList();

        // Sort the key-value pairs based on the key
        keyValuePairs.Sort((pair1, pair2) =>
        {
            var key1 = pair1.Split('=')[0];
            var key2 = pair2.Split('=')[0];
            return string.Compare(key1, key2, StringComparison.Ordinal);
        });

        // Join the sorted key-value pairs back into a single string
        string sortedRawSignature = string.Join("&", keyValuePairs);

        // Generate the HMAC hash using the sorted string
        return await Task.FromResult(HashUtils.HmacSha256(sortedRawSignature, checksumKey));
    }
}