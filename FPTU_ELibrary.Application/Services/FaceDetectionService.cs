using System.Net.Http.Headers;
using System.Text.Json;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.FaceDetection;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FPTU_ELibrary.Application.Services;

public class FaceDetectionService : IFaceDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly FaceDetectionSettings _faceDetectionSettings;
    private readonly ISystemMessageService _msgService;
    private readonly ILogger _logger;

    public FaceDetectionService(HttpClient httpClient,
        ILogger logger,
        ISystemMessageService msgService,
        IOptionsMonitor<FaceDetectionSettings> monitor)
    {
        _logger = logger;
        _msgService = msgService;
        _httpClient = httpClient;
        _faceDetectionSettings = monitor.CurrentValue;
    }
    
    public async Task<IServiceResult> DetectFaceAsync(IFormFile file, string[] attributes)
    {
        // Determine current system lang
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        if (file.Length == 0)
        {
            // Not found {0}
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "image file" : "file hình ảnh"));
        }

        try
        {
            // Initialize multipart form data content
            // Provides a container for content encoded using multipart/ form-data MIME type.
            using var formData = new MultipartFormDataContent();
            
            // Add content to collection, and name of HTTP content to add
            // API Key
            var apiKeyContent = new StringContent(_faceDetectionSettings.ApiKey);
            apiKeyContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"api_key\""
            };
            formData.Add(apiKeyContent);
            
            // API Secret
            var apiSecretContent = new StringContent(_faceDetectionSettings.ApiSecret);
            apiSecretContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"api_secret\""
            };
            formData.Add(apiSecretContent);
            
            // Return attributes
            var returnAttributesContent = new StringContent(attributes.Any() ? String.Join(",", attributes) : "gender,age");
            returnAttributesContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"return_attributes\""
            };
            formData.Add(returnAttributesContent);
            
            // Image File 
            // Initialize stream content 
            await using var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"image_file\"",
                FileName = $"\"{file.FileName}\""
            };
            // Assign header content type
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            // Add image file content
            formData.Add(fileContent);

            // Process request with POST method
            using var response = await _httpClient.PostAsync(_faceDetectionSettings.ApiUrl, formData).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) // Failed, invoke errors
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.Error($"Face detection failed: {response.StatusCode} - {errorMessage}");
                Console.WriteLine($"API Error: {errorMessage}");
                
                // Msg: failed to detect face image
                return new ServiceResult(ResultCodeConst.AIService_Warning0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0007));
            }

            // Serialize the HTTP content to a string as an asynchronous operation
            var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Parse string JSON to face detection result dto
            var result = JsonSerializer.Deserialize<FaceDetectionResultDto>(jsonResponse);
            if (result != null) 
            {
                // Msg: Success to detect face image
                return new ServiceResult(ResultCodeConst.AIService_Success0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0006), result);
            }
            
            // Msg: failed to detect face image
            return new ServiceResult(ResultCodeConst.AIService_Warning0007,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);   
            throw new Exception("Error invoke when process detect face image");
        }
    }
}