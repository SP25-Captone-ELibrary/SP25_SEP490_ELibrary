using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Services;

public class AIDetectionService : IAIDetectionService
{
    private readonly ISystemMessageService _msgService;
    private readonly HttpClient _httpClient;
    private readonly DetectSettings _monitor;
    private readonly ILogger _logger;

    public AIDetectionService(HttpClient httpClient, ISystemMessageService msgService,
        IOptionsMonitor<DetectSettings> monitor, ILogger logger)
    {
        _msgService = msgService;
        _httpClient = httpClient;
        _monitor = monitor.CurrentValue;
        _logger = logger;
    }
    public async Task<List<BoxDto>> DetectAsync(IFormFile image)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(_monitor.DetectModelUrl), "model");
            content.Add(new StringContent(_monitor.DetectImageSize.ToString()), "imgsz");
            content.Add(new StringContent(_monitor.DetectConfidence.ToString()), "conf");
            content.Add(new StringContent(_monitor.DetectIOU.ToString()), "iou");
            using var fileStream = image.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            content.Add(fileContent, "file", image.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, _monitor.DetectAPIUrl)
            {
                Content = content
            };
            request.Headers.Add("x-api-key", _monitor.DetectAPIKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var stringResponse =await response.Content.ReadAsStringAsync();

            //convert response to get boxes
            var responseConvertVersion = JsonConvert.DeserializeObject<DetectResponseDto>(stringResponse);
            var boxes = responseConvertVersion.Images
                .SelectMany(image => image.Results)
                .Where(r => r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Box)
                .ToList();
            return boxes;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Detect Book");
        }
    }
}