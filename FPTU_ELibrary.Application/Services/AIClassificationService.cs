using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Text.Json;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FPTU_ELibrary.Application.Services;

public class AIClassificationService : IAIClassificationService
{
    private readonly HttpClient _httpClient;
    private readonly CustomVisionSettings _monitor;
    private readonly IBookService<BookDto> _bookService;
    private readonly string _baseUrl;
    private readonly ILogger _logger;
    private readonly ISystemMessageService _msgService;
    private IHubContext<AiHub> _hubContext;
    private IBookEditionService<BookEditionDto> _bookEditionService;

    public AIClassificationService(HttpClient httpClient, ISystemMessageService msgService,
        IBookService<BookDto> bookService, IBookEditionService<BookEditionDto> bookEditionService,
        IHubContext<AiHub> hubContext
        , IOptionsMonitor<CustomVisionSettings> monitor, ILogger logger)
    {
        _hubContext = hubContext;
        _msgService = msgService;
        _httpClient = httpClient;
        _bookService = bookService;
        _bookEditionService = bookEditionService;
        _monitor = monitor.CurrentValue;
        _logger = logger;
        _baseUrl = string.Format(_monitor.BaseAIUrl, _monitor.TrainingEndpoint, _monitor.ProjectId);
    }

    public async Task<IServiceResult> TrainModel(List<TrainedBookDetailDto> req, string email)
    {
        try
        {
            if (!req.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0001));
            }

            //get book to find training book code
            var baseSpec = new BaseSpecification<Book>(b => b.BookEditions.Any(e
                => e.BookEditionId == req[0].BookEditionId));
            baseSpec.ApplyInclude(q => q.Include(x => x.BookEditions));
            var bookResult = await _bookService.GetWithSpecAsync(baseSpec, false);
            if (bookResult.Data is null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0001));
            }

            var book = (BookDto)bookResult.Data;
            var trainingBookCode = book.BookCodeForAITraining;

            // int images = imageList.Count;
            var projectId = Guid.Parse(_monitor.ProjectId);
            // Ensure that the tag exists for the book title
            List<TagDto> tags = await GetTagAsync();
            TagDto tag;
            if (!tags.Select(x => x.Name).ToList().Contains(book.BookCodeForAITraining.ToString()!))
            {
                tag = await CreateTagAsync(book.BookCodeForAITraining??Guid.NewGuid());    
            }
            else
            {
                tag = tags.FirstOrDefault(x => x.Name.Equals(book.BookCodeForAITraining.ToString()));
            }

            var errMsg = new List<string>();
            for (int i = 0; i < req.Count; i++)
            {
                var imageList = req[i].ImageList;
                if (imageList.Count < 4)
                {
                    errMsg.Add($"Book Edition Id: {req[i].BookEditionId} has less than 4 images");
                    continue;
                }

                var bookEdition = await _bookEditionService.GetByIdAsync(req[i].BookEditionId);
                
                // upload images with dynamic field names and filenames
                await CreateImagesFromDataAsync(imageList, tag.Id);     
            }


            // var coverImages = bookDetail.BookEditions.Select(x => x.CoverImage).ToList();
            // for (int i = 0; i<coverImages.Count;i++)
            // {
            // var response = await _httpClient.GetAsync(coverImages[i]);
            // response.EnsureSuccessStatusCode();
            //     var content = await response.Content.ReadAsByteArrayAsync();
            //     //get public id
            //     var publicId = StringUtils.GetPublicIdFromCloudinaryUrl(coverImages[i]);
            //     //create temp file name
            //     string fileName = $"{publicId}_{i}.jpg";
            //     var memoryStream = new MemoryStream(content);
            //     //Create IFormFile item
            //     var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "image", fileName)
            //     {
            //         Headers = new HeaderDictionary(),
            //         ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream"
            //     };
            //     imageList.Add(formFile);
            // }

            // TagDto tag;
            // if (bookDetail.BookCodeForAITraining is null)
            // {
            //     var trainingCode = Guid.NewGuid();
            //     
            //     tag = await CreateTagAsync(trainingCode);
            // }
            // else
            // {
            //     tag = tags.FirstOrDefault(x => x.Name.Equals(bookDetail.BookCodeForAITraining));
            // }

            // upload images with dynamic field names and filenames
            await CreateImagesFromDataAsync(imageList, tag.Id);

            // Train the model after adding the images
            var iteration = await TrainProjectAsync();

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(iteration.Id);

            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(iteration.Id);

            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(iteration.Id, "BookModel");

            //Send notification when finish
            await _hubContext.Clients.User(email).SendAsync("Trained Successfully");
            
            // Update the book to indicate that it has been trained
            var updateResult =await _bookService.UpdateTrainingStatus(bookId,new BookDto()
            {
                IsTrained = true,
                BookCodeForAITraining = bookDetail.BookCodeForAITraining
            });
            if (updateResult.ResultCode != ResultCodeConst.SYS_Success0003)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }
            return new ServiceResult(ResultCodeConst.AIService_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }

    private async Task<List<TagDto>> GetTagAsync()
    {
        try
        {
            var getTagUrl = _baseUrl + "/tags";
            _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);
            var response = await _httpClient.GetAsync(getTagUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TagDto>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Get AI Tag");
        }
    }

    private async Task<TagDto> CreateTagAsync(Guid bookCodeForTraining)
    {
        try
        {
            var createTagUrl =
                _baseUrl + $"/tags?name={Uri.EscapeDataString(bookCodeForTraining.ToString())}&type=Regular";
            _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);
            var response = await _httpClient.PostAsync(createTagUrl, null); // No content in the body
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TagDto>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when create AI Tag");
        }
    }

    private async Task CreateImagesFromDataAsync(List<IFormFile> images, Guid tagId)
    {
        try
        {
            var url = _baseUrl + $"/images?tagIds={tagId}";
            _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);

            for (int i = 0; i < images.Count; i++)
            {
                var file = images[i];

                using (var imageStream = file.OpenReadStream()) // Mở stream từ IFormFile
                {
                    var content = new MultipartFormDataContent
                    {
                        { new StreamContent(imageStream), $"files[{i}]", $"image_{i}.jpg" }
                    };

                    var response = await _httpClient.PostAsync(url, content);

                    // Kiểm tra trạng thái thành công
                    response.EnsureSuccessStatusCode();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when update image for training");
        }
    }

    private async Task<IterationDto> TrainProjectAsync()
    {
        var trainUrl = _baseUrl + "/train";
        _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);
        var response = await _httpClient.PostAsync(trainUrl, null);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IterationDto>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task WaitForTrainingCompletionAsync(Guid iterationId)
    {
        try
        {
            var checkIterationUrl = _baseUrl + $"/iterations/{iterationId}";
            _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);

            // Polling loop to check training status
            while (true)
            {
                var response = await _httpClient.GetAsync(checkIterationUrl);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var iteration = JsonSerializer.Deserialize<IterationDto>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                // Check if the training status is completed
                if (iteration.Status.Equals(nameof(IterationStatus.Completed)))
                {
                    break;
                }

                // Wait for a few seconds before checking again
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when checking iteration while training");
        }
    }

    private async Task UnpublishPreviousIterationAsync(Guid iterationId)
    {
        try
        {
            var getIterationUrl = _baseUrl + "/iterations";
            var response = await _httpClient.GetAsync(getIterationUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var iterations = JsonSerializer.Deserialize<List<IterationDto>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Find the previous version to delete
            var publishedIteration = iterations?.Where(iter => iter.Id != iterationId).ToList();

            if (publishedIteration.Any())
            {
                foreach (var iteration in publishedIteration)
                {
                    if (iteration.PublishName is not null)
                    {
                        // Unpublish the current iteration
                        await UnpublishIterationAsync(iteration.Id);
                    }

                    // Optionally delete the unpublished iteration
                    await DeleteIterationAsync(iteration.Id);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task UnpublishIterationAsync(Guid iterationId)
    {
        var getPublishIteration = _baseUrl + $"/iterations/{iterationId}/publish";
        _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);

        var response = await _httpClient.DeleteAsync(getPublishIteration);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteIterationAsync(Guid iterationId)
    {
        var deleteIterationUrl = _baseUrl + $"/iterations/{iterationId}";
        _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);

        var response = await _httpClient.DeleteAsync(deleteIterationUrl);
        response.EnsureSuccessStatusCode();
    }

    private async Task PublishIterationAsync(Guid iterationId, string publishName)
    {
        var predictionQuery =
            $"/subscriptions/{_monitor.SubscriptionKey}/resourceGroups/{_monitor.ResourceGroup}/providers/{_monitor.Provider}/accounts/{_monitor.Account}";
        var encodedQuery = Uri.EscapeDataString(predictionQuery);
        var url = _baseUrl + $"/iterations/{iterationId}/publish?predictionId={encodedQuery}&publishName={publishName}";
        _httpClient.DefaultRequestHeaders.Add("Training-Key", _monitor.TrainingKey);

        var response = await _httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();
    }
}