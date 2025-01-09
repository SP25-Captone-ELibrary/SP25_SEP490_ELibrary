using System.Net.Http.Headers;
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
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Dtos.AIServices.Speech;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Utils;
using SixLabors.ImageSharp;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Processing;
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
    private readonly IServiceProvider _service;
    private readonly IAIDetectionService _aiDetectionService;
    private readonly string _basePredictUrl;
    private readonly IOCRService _ocrService;

    public AIClassificationService(HttpClient httpClient, ISystemMessageService msgService,
        IBookService<BookDto> bookService, IBookEditionService<BookEditionDto> bookEditionService,
        IHubContext<AiHub> hubContext, IAIDetectionService aiDetectionService
        , IOptionsMonitor<CustomVisionSettings> monitor, ILogger logger,
        IServiceProvider service, IOCRService ocrService)
    {
        _ocrService = ocrService;
        _aiDetectionService = aiDetectionService;
        _hubContext = hubContext;
        _msgService = msgService;
        _httpClient = httpClient;
        _bookService = bookService;
        _bookEditionService = bookEditionService;
        _monitor = monitor.CurrentValue;
        _logger = logger;
        _service = service;
        _baseUrl = string.Format(_monitor.BaseAIUrl, _monitor.TrainingEndpoint, _monitor.ProjectId);
        _basePredictUrl = string.Format(_monitor.BasePredictUrl, _monitor.PredictionEndpoint, _monitor.ProjectId,
            _monitor.PublishedName);
    }

    public async Task<IServiceResult> PredictAsync(IFormFile image)
    {
        try
        {
            // Detect bounding boxes for books
            var bookBoxes = await _aiDetectionService.DetectAsync(image);

            if (!bookBoxes.Any())
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
            }

            // Crop images based on bounding boxes
            using (var imageStream = image.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var croppedImages = CropImages(imageBytes, bookBoxes);
                List<Guid> bookCodes = new List<Guid>();
                var predictResponse = new PredictionResponseDto()
                {
                    NumberOfBookDetected = croppedImages.Count,
                    BookEditionPrediction = new List<PossibleBookEdition>()
                };
                // detect and predict base on cropped images
                foreach (var croppedImage in croppedImages)
                {
                    var content = new ByteArrayContent(croppedImage);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                    var response = await _httpClient.PostAsync(_basePredictUrl, content);
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });
                    var bestPrediction =
                        predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
                    var baseSpec = new BaseSpecification<Book>(x =>
                        x.BookCodeForAITraining.ToString().ToLower().Equals(bestPrediction.TagName));
                    baseSpec.ApplyInclude(q =>
                        q.Include(x => x.BookEditions)
                            .ThenInclude(be => be.BookEditionAuthors)
                            .ThenInclude(ea => ea.Author));
                    var bookSearchResult = await _bookService.GetWithSpecAsync(baseSpec);
                    if (bookSearchResult.ResultCode != ResultCodeConst.SYS_Success0002)
                    {
                        return bookSearchResult;
                    }

                    var book = (BookDto)bookSearchResult.Data!;
                    var availableEdition = new List<BookEditionDto>();

                    foreach (var edition in book.BookEditions)
                    {
                        var stream = new MemoryStream(croppedImage);
                        var bookInfo = new CheckedBookEditionDto()
                        {
                            Title = edition.EditionTitle,
                            Authors = edition.BookEditionAuthors.Where(x => x.BookEditionId == edition.BookEditionId)
                                .Select(x => x.Author.FullName).ToList(),
                            Publisher = edition.Publisher ?? " ",
                            Image = new FormFile(stream, 0, stream.Length, "file", edition.EditionTitle
                                + edition.EditionNumber)
                            {
                                Headers = new HeaderDictionary(),
                                ContentType = "application/octet-stream"
                            }
                        };
                        var compareResult = await _ocrService.CheckBookInformationAsync(bookInfo);
                        var compareResultValue = (MatchResultDto)compareResult.Data!;
                        if (compareResultValue.TotalPoint > compareResultValue.ConfidenceThreshold)
                        {
                            availableEdition.Add(edition);
                        }
                    }

                    //add available edition to response
                    predictResponse.BookEditionPrediction.Add(new PossibleBookEdition()
                    {
                        BookEditionDetails = availableEdition,
                        BookCode = book.BookCodeForAITraining.ToString()
                    });
                }

                return new ServiceResult(ResultCodeConst.AIService_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003), predictResponse
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Predict Book Model");
        }
    }

    public async Task<IServiceResult> Recommendation(IFormFile image)
    {
        try
        {
            // detect bounding boxes for books and check if it is more than 2 books
            var bookBoxes = await _aiDetectionService.DetectAsync(image);
            if (!bookBoxes.Any())
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
            }

            if (bookBoxes.Count > 1)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
            }

            //crop image to get the picture of the book only
            using (var imageStream = image.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var croppedImage = CropImages(imageBytes, bookBoxes).First();

                //using the cropped image to predict book
                var content = new ByteArrayContent(croppedImage);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                var response = await _httpClient.PostAsync(_basePredictUrl, content);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
                var bestPrediction =
                    predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
                var baseSpec = new BaseSpecification<Book>(x =>
                    x.BookCodeForAITraining.ToString().ToLower().Equals(bestPrediction.TagName));
                baseSpec.ApplyInclude(q =>
                    q.Include(x => x.BookEditions)
                        .ThenInclude(be => be.BookEditionAuthors)
                        .ThenInclude(ea => ea.Author));
                var bookSearchResult = await _bookService.GetWithSpecAsync(baseSpec);
                if (bookSearchResult.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return bookSearchResult;
                }

                var book = (BookDto)bookSearchResult.Data!;
                var relatedBookEdition = new List<RelatedItemDto<BookEditionDto>>();
                //check the exact edition in the book
                foreach (var edition in book.BookEditions)
                {
                    var stream = new MemoryStream(croppedImage);
                    var bookInfo = new CheckedBookEditionDto()
                    {
                        Title = edition.EditionTitle,
                        Authors = edition.BookEditionAuthors.Where(x => x.BookEditionId == edition.BookEditionId)
                            .Select(x => x.Author.FullName).ToList(),
                        Publisher = edition.Publisher ?? " ",
                        Image = new FormFile(stream, 0, stream.Length, "file", edition.EditionTitle
                                                                               + edition.EditionNumber)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = "application/octet-stream"
                        }
                    };

                    var compareResult = await _ocrService.CheckBookInformationAsync(bookInfo);
                    var compareResultValue = (MatchResultDto)compareResult.Data!;
                    if (compareResultValue.TotalPoint > compareResultValue.ConfidenceThreshold)
                    {
                        var sameAuthorEditions =
                            await _bookEditionService.GetRelatedEditionWithMatchField(edition, nameof(Author));
                        relatedBookEdition.Add(new RelatedItemDto<BookEditionDto>()
                        {
                            RelatedProperty = nameof(Author),
                            RelatedItems = sameAuthorEditions.Data as List<BookEditionDto>
                        });

                       var sameCategoryEditions = await _bookEditionService
                           .GetRelatedEditionWithMatchField(edition, nameof(Category));
                        relatedBookEdition.Add(new RelatedItemDto<BookEditionDto>()
                        {
                            RelatedItems = sameCategoryEditions.Data as List<BookEditionDto>,
                            RelatedProperty = nameof(Category)
                        });
                    }
                }

                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    relatedBookEdition);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when giving recommendation Book Model");
        }
    }

    public async Task<IServiceResult> TrainModelAfterCreate(Guid bookCode, List<IFormFile> images, string email)
    {
        // Get process message first
        var successMessage = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003);

        // Run background task
        var backgroundTask = Task.Run(() => ProcessTrainingTask(bookCode, images, email, null));

        // Return ServiceResult
        var result = new ServiceResult(ResultCodeConst.AIService_Success0003, successMessage);

        // Make sure background Task work after return
        _ = backgroundTask;

        return result;
    }

    public async Task<IServiceResult> TrainModelWithoutCreate(int editionId, List<IFormFile> images, string email)
    {
        // Get process message first
        var successMessage = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003);

        var baseSpec = new BaseSpecification<Book>(x => (x.BookEditions.Select(e
            => e.BookEditionId == editionId).FirstOrDefault()));

        baseSpec.ApplyInclude(q => q.Include(x => x.BookEditions));

        var bookCode = ((await _bookService.GetWithSpecAsync(baseSpec)).Data as BookDto)!.BookCodeForAITraining ??
                       new Guid();

        // Run background task
        var backgroundTask = Task.Run(() => ProcessTrainingTask(bookCode, images, email, editionId));

        // Return ServiceResult
        var result = new ServiceResult(ResultCodeConst.AIService_Success0003, successMessage);

        // Make sure background Task work after return 
        _ = backgroundTask;

        return result;
    }


    private async Task ProcessTrainingTask(Guid bookCode, List<IFormFile> images, string email, int? editionId)
    {
        // Save images to memoryStream


        // define services that use in background task
        using var scope = _service.CreateScope();
        var bookService = scope.ServiceProvider.GetRequiredService<IBookService<BookDto>>();
        var bookEditionService = scope.ServiceProvider.GetRequiredService<IBookEditionService<BookEditionDto>>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AiHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
        var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CustomVisionSettings>>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // define monitor value
        var currentAiConfiguration = monitor.CurrentValue;
        try
        {
            // save IFormFile to memoryStream
            var memoryStreams = new List<(MemoryStream Stream, string FileName)>();
            foreach (var file in images)
            {
                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                memoryStreams.Add((memoryStream, file.FileName ?? $"image_{memoryStreams.Count}.jpg"));
            }

            var baseConfig = new BaseConfigurationBackgroudDto
            {
                Client = httpClient,
                Configuration = currentAiConfiguration,
                Logger = logger,
                BaseUrl = string.Format(monitor.CurrentValue.BaseAIUrl,
                    monitor.CurrentValue.TrainingEndpoint,
                    monitor.CurrentValue.ProjectId)
            };
            // Ensure that the tag exists for the book title

            List<TagDto> tags = await GetTagAsync(baseConfig);

            TagDto tag;
            if (!tags.Select(x => x.Name).ToList().Contains(bookCode.ToString()!))
            {
                tag = await CreateTagAsync(baseConfig, bookCode);
            }
            else
            {
                tag = tags.FirstOrDefault(x => x.Name.Equals(bookCode.ToString()));
            }

            //check if it is in create process or not
            if (editionId is null)
            {
                //get book to find training book code
                var baseSpec = new BaseSpecification<Book>(b => b.BookCodeForAITraining == bookCode);
                baseSpec.ApplyInclude(q => q.Include(x => x.BookEditions));
                var bookResult = await bookService.GetWithSpecAsync(baseSpec, false);
                if (bookResult.Data is null)
                {
                    await hubContext.Clients.User(email).SendAsync("Cannot define book");
                    return;
                }

                var book = (BookDto)bookResult.Data;

                var bookEditions = book.BookEditions.ToList();

                for (int i = 0; i < bookEditions.Count; i++)
                {
                    var coverImage = bookEditions[i].CoverImage;
                    var response = await httpClient.GetAsync(coverImage);
                    // using response.IsSuccessStatusCode to check if the request is successful
                    if (!response.IsSuccessStatusCode)
                    {
                        await hubContext.Clients.User(email).SendAsync("Get cover image unsuccessfully");
                        return;
                    }

                    var memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                    memoryStream.Position = 0;
                    memoryStreams.Add((memoryStream, $"{bookEditions[i].BookEditionId}_cover.jpg"));
                }
            }
            else
            {
                var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == editionId);
                var bookEditionsResult = await bookEditionService.GetWithSpecAsync(baseSpec);
                var bookEdition = (BookEditionDto)bookEditionsResult.Data!;
                var coverImage = bookEdition.CoverImage;
                var response = await httpClient.GetAsync(coverImage);
                // using response.IsSuccessStatusCode to check if the request is successful
                if (!response.IsSuccessStatusCode)
                {
                    await hubContext.Clients.User(email).SendAsync("Get cover image unsuccessfully");
                    return;
                }

                var memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                memoryStream.Position = 0;
                memoryStreams.Add((memoryStream, $"{bookEdition.BookEditionId}_cover.jpg"));
            }


            // upload images with dynamic field names and filenames
            await CreateImagesFromDataAsync(baseConfig, memoryStreams, tag.Id);

            // Train the model after adding the images
            var iteration = await TrainProjectAsync(baseConfig);

            if (iteration is null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            }

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(baseConfig, iteration.Id);

            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(baseConfig, iteration.Id);

            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(baseConfig, iteration.Id, monitor.CurrentValue.PublishedName);

            //Change training status in book edition

            await bookEditionService.UpdateTrainingStatusAsync(bookCode);

            //Send notification when finish
            await hubContext.Clients.User(email).SendAsync("Trained Successfully");
        }
        catch (Exception ex)
        {
            await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            logger.Error(ex.Message);
            throw new Exception("Error invoke when Train Book Model");
        }
    }

    private async Task<List<TagDto>> GetTagAsync(BaseConfigurationBackgroudDto dto)
    {
        try
        {
            var getTagUrl = dto.BaseUrl + "/tags";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);
            var response = await dto.Client.GetAsync(getTagUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TagDto>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when Get AI Tag");
        }
    }

    private async Task<TagDto> CreateTagAsync(BaseConfigurationBackgroudDto dto, Guid bookCodeForTraining)
    {
        try
        {
            var createTagUrl =
                dto.BaseUrl + $"/tags?name={Uri.EscapeDataString(bookCodeForTraining.ToString())}&type=Regular";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);
            var response = await dto.Client.PostAsync(createTagUrl, null); // No content in the body
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TagDto>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when create AI Tag");
        }
    }

    private async Task CreateImagesFromDataAsync(BaseConfigurationBackgroudDto dto,
        List<(MemoryStream Stream, string FileName)> images, Guid tagId)
    {
        try
        {
            var url = dto.BaseUrl + $"/images?tagIds={tagId}";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);

            using var multipartContent = new MultipartFormDataContent();

            foreach (var (stream, fileName) in images)
            {
                multipartContent.Add(new StreamContent(stream), "files", fileName);
            }

            var response = await dto.Client.PostAsync(url, multipartContent);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex, "Error occurred while uploading images.");
            throw new Exception("Failed to upload images.");
        }
        finally
        {
            // make sure that MemoryStream is disposed
            foreach (var (stream, _) in images)
            {
                stream.Dispose();
            }
        }
    }

    private async Task<IterationDto?> TrainProjectAsync(BaseConfigurationBackgroudDto dto)
    {
        try
        {
            var trainUrl = dto.BaseUrl + "/train";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);
            var response = await dto.Client.PostAsync(trainUrl, null);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IterationDto>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when training");
        }
    }

    private async Task WaitForTrainingCompletionAsync(BaseConfigurationBackgroudDto dto, Guid iterationId)
    {
        try
        {
            var checkIterationUrl = dto.BaseUrl + $"/iterations/{iterationId}";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);

            // Polling loop to check training status
            while (true)
            {
                var response = await dto.Client.GetAsync(checkIterationUrl);
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
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when waiting for training completion");
        }
    }

    private async Task UnpublishPreviousIterationAsync(BaseConfigurationBackgroudDto dto, Guid iterationId)
    {
        try
        {
            var getIterationUrl = dto.BaseUrl + "/iterations";
            var response = await dto.Client.GetAsync(getIterationUrl);
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
                        await UnpublishIterationAsync(dto, iteration.Id);
                    }

                    // Optionally delete the unpublished iteration
                    await DeleteIterationAsync(dto, iteration.Id);
                }
            }
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when unpublishing previous iteration");
        }
    }

    private async Task UnpublishIterationAsync(BaseConfigurationBackgroudDto dto, Guid iterationId)
    {
        try
        {
            var getPublishIteration = dto.BaseUrl + $"/iterations/{iterationId}/publish";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);

            var response = await dto.Client.DeleteAsync(getPublishIteration);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when unpublishing iteration");
        }
    }

    private async Task DeleteIterationAsync(BaseConfigurationBackgroudDto dto, Guid iterationId)
    {
        try
        {
            var deleteIterationUrl = dto.BaseUrl + $"/iterations/{iterationId}";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);

            var response = await dto.Client.DeleteAsync(deleteIterationUrl);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when deleting iteration");
        }
    }

    private async Task PublishIterationAsync(BaseConfigurationBackgroudDto dto, Guid iterationId, string publishName)
    {
        try
        {
            var predictionQuery =
                $"/subscriptions/{dto.Configuration.SubscriptionKey}/resourceGroups/{dto.Configuration.ResourceGroup}" +
                $"/providers/{dto.Configuration.Provider}/accounts/{dto.Configuration.Account}";
            var encodedQuery = Uri.EscapeDataString(predictionQuery);
            var url = dto.BaseUrl +
                      $"/iterations/{iterationId}/publish?predictionId={encodedQuery}&publishName={publishName}";
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);

            var response = await dto.Client.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            dto.Logger.Error(ex.Message);
            throw new Exception("Error invoke when publishing iteration");
        }
    }

    private List<byte[]> CropImages(byte[] imageBytes, List<BoxDto> boxes)
    {
        var croppedImages = new List<byte[]>();
        using (var image = SixLabors.ImageSharp.Image.Load(imageBytes))
        {
            foreach (var box in boxes)
            {
                // Rectangle of book cover 
                var cropReg = new Rectangle((int)Math.Floor(box.X1), (int)Math.Floor(box.Y1),
                    (int)Math.Ceiling(box.X2 - box.X1)
                    , (int)Math.Ceiling(box.Y2 - box.Y1));
                using (var cropped = image.Clone(ctx => ctx.Crop(cropReg)))
                {
                    using (var ms = new MemoryStream())
                    {
                        cropped.SaveAsJpeg(ms);
                        croppedImages.Add(ms.ToArray());
                    }
                }
            }
        }

        return croppedImages;
    }
}