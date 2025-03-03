using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using Net.payOS.Types;

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
    public static PayOSPaymentLinkInformationResponseDto ToPayOsResponseDto(this WebhookType webhookType,
        TransactionStatus transactionStatus)
    {
        return new()
        {
            Code = webhookType.code,
            Desc = webhookType.desc,
            Signature = webhookType.signature,
            Data = new ()
            {
                OrderCode = webhookType.data.orderCode.ToString(),
                Amount = webhookType.data.amount,
                Id = webhookType.data.paymentLinkId,
                Status = transactionStatus.ToString().ToUpperInvariant(),
                AmountPaid = webhookType.data.amount,
                Transactions = [
                    new()
                    {
                        Reference = webhookType.data.reference,
                        Amount = webhookType.data.amount,
                        AccountNumber = webhookType.data.accountNumber,
                        TransactionDateTime = webhookType.data.transactionDateTime,
                        VirtualAccountName = webhookType.data.virtualAccountName,
                        VirtualAccountNumber = webhookType.data.virtualAccountNumber,
                        CounterAccountBankId = webhookType.data.counterAccountBankId,
                        CounterAccountBankName = webhookType.data.counterAccountBankName,
                        CounterAccountName = webhookType.data.counterAccountName,
                        CounterAccountNumber = webhookType.data.counterAccountNumber,
                        Description = webhookType.data.description,
                    }
                ]
            }
        };
    }
    
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