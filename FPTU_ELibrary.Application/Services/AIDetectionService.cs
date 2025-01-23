using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Headers;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Services;

public class AIDetectionService : IAIDetectionService
{
    private readonly ISystemMessageService _msgService;
    private readonly HttpClient _httpClient;
    private readonly DetectSettings _monitor;
    private readonly ILogger _logger;
    private readonly IOCRService _ocrService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGroupService;

    public AIDetectionService(HttpClient httpClient, ISystemMessageService msgService,
        IOptionsMonitor<DetectSettings> monitor, ILogger logger, IOCRService ocrService,
        ILibraryItemService<LibraryItemDto> libraryItemService
        , ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGroupService)
    {
        _msgService = msgService;
        _httpClient = httpClient;
        _monitor = monitor.CurrentValue;
        _logger = logger;
        _ocrService = ocrService;
        _libraryItemService = libraryItemService;
        _libraryItemGroupService = libraryItemGroupService;
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
            var stringResponse = await response.Content.ReadAsStringAsync();

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

    private async Task<List<DetectResultDto>> DetectAllAsync(IFormFile image)
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
            var stringResponse = await response.Content.ReadAsStringAsync();

            //convert response to get boxes
            var responseConvertVersion = JsonConvert.DeserializeObject<DetectResponseDto>(stringResponse);
            var boxes = responseConvertVersion.Images
                .SelectMany(image => image.Results)
                .ToList();
            return boxes;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Detect Book");
        }
    }

    /// <summary>
    /// compare input training images themselves and
    /// with current items in group if it is existed
    /// </summary>
    /// <param name="currentItemsDetail">Current group details with itemIds and coverImages</param>
    /// <param name="compareList"></param>
    /// <returns></returns>
    public async Task<IServiceResult> ValidateImportTraining(List<int> itemIds, List<IFormFile> compareList)
    {
        try
        {
            // Initialize dictionaries to track results
            Dictionary<string, List<bool>> comparerResults = new Dictionary<string, List<bool>>();
            Dictionary<string, string> notMatchImages = new Dictionary<string, string>();

            foreach (var comparer in compareList)
            {
                comparerResults[comparer.FileName] = new List<bool>();
            }

            // Initialize the response DTO
            var response = new CheckDuplicateImageDto<string>
            {
                ObjectMatchResult = new List<ObjectMatchResultDto<string>>(),
                OCRResult = new List<MatchResultDto>()
            };

            foreach (int itemId in itemIds)
            {
                // Get item details
                var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == itemId);
                baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors).ThenInclude(lia => lia.Author));
                var itemDetail = await _libraryItemService.GetWithSpecAsync(baseSpec);
                if (itemDetail.Data is null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "book"));
                }

                var foundItem = (LibraryItemDto)itemDetail.Data!;

                // OCR Check
                var ocrCheckedResult = await _ocrService.CheckBookInformationAsync(new CheckedItemDto()
                {
                    Title = foundItem.Title,
                    SubTitle = foundItem.SubTitle,
                    Publisher = foundItem.Publisher,
                    Authors = foundItem.LibraryItemAuthors.Select(lia => lia.Author.FullName).ToList(),
                    Images = compareList,
                    GeneralNote = foundItem.GeneralNote
                });

                var ocrCheckedResultDetails = (List<MatchResultDto>)ocrCheckedResult.Data!;
                response.OCRResult.AddRange(ocrCheckedResultDetails);

                // Identify images that failed OCR
                var notPassedOcrCheckedItems = ocrCheckedResultDetails
                    .Where(mr => mr.TotalPoint < mr.ConfidenceThreshold)
                    .Select(mr => mr.ImageName)
                    .ToList();

                foreach (var imageName in notPassedOcrCheckedItems)
                {
                    notMatchImages[imageName] = "OCR Err";
                    comparerResults[imageName].Add(false);
                }

                // Get cover image and detect objects
                var coverImageData = await _httpClient.GetByteArrayAsync(foundItem.CoverImage);
                var stream = new MemoryStream(coverImageData);
                var file = new FormFile(stream, 0, stream.Length, "file", foundItem.Title + ".jpg")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/octet-stream"
                };

                var coverDetectedResults = await DetectAllAsync(file);
                var bookBox = coverDetectedResults
                    .Where(r => r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Box)
                    .FirstOrDefault();

                if (bookBox == null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0003, "No book detected in cover image.");
                }

                var coverObjectCounts = CountObjectsInImage(coverDetectedResults, bookBox);

                foreach (var comparer in compareList)
                {
                    var objectMatchResult = new ObjectMatchResultDto<string>
                    {
                        ImageName = comparer.FileName,
                        ObjectMatchResults = new List<BaseObjectMatchResultDto<string>>()
                    };

                    if (notMatchImages.ContainsKey(comparer.FileName))
                    {
                        objectMatchResult.ObjectMatchResults.Add(new BaseObjectMatchResultDto<string>
                        {
                            ObjectType = null,
                            NumberOfObject = 0,
                            IsPassed = false
                        });
                        response.ObjectMatchResult.Add(objectMatchResult);
                        
                        continue;
                    }

                    // Detect objects in comparer image
                    var comparerDetectedResults = await DetectAllAsync(comparer);
                    var comparerObjectCounts = CountObjectsInImage(comparerDetectedResults, bookBox);

                    bool isMatched = true;

                    foreach (var comparerObject in comparerObjectCounts.Keys)
                    {
                        var baseObjectMatchResult = new BaseObjectMatchResultDto<string>
                        {
                            ObjectType = comparerObject,
                            NumberOfObject = comparerObjectCounts[comparerObject],
                            IsPassed = CompareObjectCounts(coverObjectCounts, comparerObjectCounts)
                        };

                        if (!baseObjectMatchResult.IsPassed)
                        {
                            isMatched = false;
                        }

                        objectMatchResult.ObjectMatchResults.Add(baseObjectMatchResult);
                    }

                    if (!comparerObjectCounts.Any())
                    {
                        var baseObjectMatchResult = new BaseObjectMatchResultDto<string>
                        {
                            ObjectType = null,
                            NumberOfObject = 0,
                            IsPassed = false
                        };
                    }

                    comparerResults[comparer.FileName].Add(isMatched);
                    response.ObjectMatchResult.Add(objectMatchResult);

                    if (!isMatched)
                    {
                        notMatchImages[comparer.FileName] = "Not Matched Image";
                    }
                }
            }

            // Final evaluation of images
            foreach (var comparer in comparerResults.Keys)
            {
                if (comparerResults[comparer].Any(r => r))
                {
                    notMatchImages.Remove(comparer);
                }
            }

            if (notMatchImages.Count == 0)
            {
                return new ServiceResult(ResultCodeConst.AIService_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001), response);
            }

            return new ServiceResult(
                ResultCodeConst.SYS_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0001),
                response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when checking validation of import training images");
        }
    }

    // public async Task<IServiceResult> CheckDuplicationOfGroup(List<int> itemIds, int groupId)
    // {
    //     try
    //     {
    //         var baseSpec = new BaseSpecification<LibraryItemGroup>(g => g.GroupId == groupId);
    //         baseSpec.ApplyInclude(q => q.Include(g => g.LibraryItems));
    //         var groupDetail = await _libraryItemGroupService.GetWithSpecAsync(baseSpec);
    //         if (groupDetail.Data is null)
    //         {
    //             return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                 StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002)
    //                     , "group"));
    //         }
    //
    //         var foundGroup = (LibraryItemGroupDto)groupDetail.Data!;
    //         var groupCoverImages = foundGroup.LibraryItems.Select(li => li.CoverImage).ToList();
    //
    //
    //         //get cover image in cloudinary
    //         var coverImageData = await _httpClient.GetByteArrayAsync(foundGroup.LibraryItems.First().CoverImage);
    //
    //         var stream = new MemoryStream(coverImageData);
    //         var file = new FormFile(stream, 0, stream.Length, "file", foundGroup.Title + ".jpg")
    //         {
    //             Headers = new HeaderDictionary(),
    //             ContentType = "application/octet-stream"
    //         };
    //
    //         // Detect objects in the cover image
    //         var coverDetectedResults = await DetectAllAsync(file);
    //
    //         // Lấy box đại diện cho quyển sách
    //         var bookBox = coverDetectedResults
    //             .Where(r => r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
    //             .Select(r => r.Box)
    //             .FirstOrDefault();
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when checking duplication of group");
    //     }
    // }


    /// <summary>
    /// Check and detect the object that in the cover image
    /// </summary>
    /// <param name="detectedResults"></param>
    /// <param name="bookBox"></param>
    /// <returns></returns>
    private Dictionary<string, int> CountObjectsInImage(List<DetectResultDto> detectedResults, BoxDto bookBox)
    {
        var objectCounts = new Dictionary<string, int>();

        foreach (var result in detectedResults)
        {
            // Only check the object that inside the book cover image
            if (result.Box.X1 > bookBox.X1 && result.Box.X2 < bookBox.X2 &&
                result.Box.Y1 > bookBox.Y1 && result.Box.Y2 < bookBox.Y2)
            {
                if (objectCounts.ContainsKey(result.Name))
                {
                    objectCounts[result.Name]++;
                }
                else
                {
                    objectCounts[result.Name] = 1;
                }
            }
        }

        return objectCounts;
    }

    /// <summary>
    /// Check if two pictures have the same object type and quantity
    /// </summary>
    /// <param name="coverCounts"></param>
    /// <param name="comparerCounts"></param>
    /// <returns></returns>
    private bool CompareObjectCounts(Dictionary<string, int> coverCounts, Dictionary<string, int> comparerCounts)
    {
        foreach (var (objectName, coverCount) in coverCounts)
        {
            if (!comparerCounts.TryGetValue(objectName, out var comparerCount) || comparerCount < coverCount)
            {
                return false;
            }
        }

        return true;
    }
}