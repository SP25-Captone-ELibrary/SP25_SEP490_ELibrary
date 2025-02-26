using System.Text;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Payments.PayOS;

public class PayOSPaymentRequestDto
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
public static class PayOsPaymentRequestExtensions
{
    public static async Task GenerateSignatureAsync(this PayOSPaymentRequestDto req, int orderCode,
        PayOSSettings payOsConfig)
    {
        var rawSignature = $"amount={req.Amount}&cancelUrl={payOsConfig.CancelUrl}&description={req.Description}" +
                           $"&orderCode={orderCode}&returnUrl={payOsConfig.ReturnUrl}";
        req.Signature = HashUtils.HmacSha256(rawSignature, payOsConfig.ChecksumKey);
        await Task.CompletedTask;
    }
    
    public static async Task<(bool, string?, PayOSPaymentResponseDto?)> GetUrlAsync(this PayOSPaymentRequestDto req, 
        PayOSSettings payOsConfig)
    {
        // Initiate HttpClient
        using HttpClient httpClient = new();
        
        // Add header parameters
        httpClient.DefaultRequestHeaders.Add("x-client-id", payOsConfig.ClientId);
        httpClient.DefaultRequestHeaders.Add("x-api-key", payOsConfig.ApiKey);
        
        // Convert request data to type of JSON
        var requestData = JsonConvert.SerializeObject(req, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        });
        // Initiate string content with serialized request data, encoding and media type
        var requestContent = new StringContent(
            content: requestData,
            encoding: Encoding.UTF8,
            mediaType: "application/json");
        // Execute POST request with uri and request content
        var createPaymentUrlRes = await httpClient.PostAsync(
            requestUri: payOsConfig.PaymentUrl, 
            content: requestContent);
        
        // Response content
        var content = createPaymentUrlRes.Content.ReadAsStringAsync().Result;
        var responseData = JsonConvert.DeserializeObject<PayOSPaymentResponseDto>(content);
        // Check for response content not found 
        if (responseData == null) return (false, "Request to server failed. Not found any response data", null!);
        
        if (createPaymentUrlRes.IsSuccessStatusCode)
        {
            return (true, string.Empty, responseData);
        }
        
        // 409: Too many request to PayOS server 
        return (false, "Too many request", responseData);
    }

    public static async Task<(bool, string?, PayOSPaymentLinkInformationResponseDto?)> GetLinkInformationAsync(string paymentLinkId,
        PayOSSettings payOsConfig)
    {
        // Initiate HttpClient
        using HttpClient httpClient = new();
        
        // Add header parameters
        httpClient.DefaultRequestHeaders.Add("x-client-id", payOsConfig.ClientId);
        httpClient.DefaultRequestHeaders.Add("x-api-key", payOsConfig.ApiKey);
        
        // Add params to Url by formating string
        var getPaymentLinkInformationUrl = string.Format(payOsConfig.GetPaymentLinkInformationUrl, paymentLinkId);
        // Execute GET request with uri 
        var createPaymentUrlRes = await httpClient.GetAsync(
            requestUri: getPaymentLinkInformationUrl);
        
        // Response content
        var content = createPaymentUrlRes.Content.ReadAsStringAsync().Result;
        var responseData = JsonConvert.DeserializeObject<PayOSPaymentLinkInformationResponseDto>(content);
        // Check for response content not found 
        if (responseData == null) return (false, "Request to server failed. Not found any response data", null!);
        
        if (createPaymentUrlRes.IsSuccessStatusCode)
        {
            return (true, string.Empty, responseData);
        }
        
        // 409: Too many request to PayOS server 
        return (false, "Too many request", responseData);
    }
}