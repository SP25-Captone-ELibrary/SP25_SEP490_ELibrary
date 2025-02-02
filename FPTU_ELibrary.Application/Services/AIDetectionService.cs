using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
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
using OpenCvSharp;

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
    public async Task<IServiceResult> ValidateImportTraining(int itemId, List<IFormFile> compareList)
    {
        try
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
            return new ServiceResult(ResultCodeConst.AIService_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0001)
                , ocrCheckedResult);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process checking training book information");
        }
    }

    // public async Task<bool> HasTheSameCoverImage(string coverImage, List<string> imagesUrl)
    // {
    //     try
    //     {
    //         var coverImageData = await _httpClient.GetByteArrayAsync(coverImage);
    //         var stream = new MemoryStream(coverImageData);
    //         var file = new FormFile(stream, 0, stream.Length, "file", "cover.jpg")
    //         {
    //             Headers = new HeaderDictionary(),
    //             ContentType = "application/octet-stream"
    //         };
    //
    //         var coverDetectedResults = await DetectAllAsync(file);
    //         var objectBox = coverDetectedResults
    //             // .Where(r => r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
    //             .Select(r => r.Box)
    //             // .FirstOrDefault();
    //             .ToList();
    //
    //         if (coverDetectedResults.Any(r =>
    //                 r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
    //         {
    //             var bookBox = coverDetectedResults.Where(r =>
    //                     r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
    //                 .Select(r => r.Box)
    //                 .FirstOrDefault();
    //             var coverObjectCounts = CountObjectsInImage(coverDetectedResults, bookBox);
    //             foreach (var imageUrl in imagesUrl)
    //             {
    //                 var comparerImageData = await _httpClient.GetByteArrayAsync(imageUrl);
    //                 var comparerStream = new MemoryStream(comparerImageData);
    //                 var comparerFile = new FormFile(comparerStream, 0, comparerStream.Length, "file", "comparer.jpg")
    //                 {
    //                     Headers = new HeaderDictionary(),
    //                     ContentType = "application/octet-stream"
    //                 };
    //
    //                 var comparerDetectedResults = await DetectAllAsync(comparerFile);
    //                 var comparerObjectCounts = CountObjectsInImage(comparerDetectedResults, bookBox);
    //
    //                 if (!CompareObjectCounts(coverObjectCounts, comparerObjectCounts))
    //                 {
    //                     return false;
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             var coverObjectCounts = CountObjectsInImage(coverDetectedResults, null);
    //             foreach (var imageUrl in imagesUrl)
    //             {
    //                 var comparerImageData = await _httpClient.GetByteArrayAsync(imageUrl);
    //                 var comparerStream = new MemoryStream(comparerImageData);
    //                 var comparerFile = new FormFile(comparerStream, 0, comparerStream.Length, "file", "comparer.jpg")
    //                 {
    //                     Headers = new HeaderDictionary(),
    //                     ContentType = "application/octet-stream"
    //                 };
    //
    //                 var comparerDetectedResults = await DetectAllAsync(comparerFile);
    //                 var comparerObjectCounts = CountObjectsInImage(comparerDetectedResults, null);
    //
    //                 if (!CompareObjectCounts(coverObjectCounts, comparerObjectCounts))
    //                 {
    //                     return false;
    //                 }
    //             }
    //         }
    //         return true;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when checking duplication of cover image");
    //     }
    // }
    // public async Task<bool> HasTheSameCoverImage(string coverImage, List<string> imagesUrl)
    // {
    //     try
    //     {
    //         var coverImageGetResponse = await _httpClient.GetAsync(coverImage);
    //         var coverImageBytes = coverImageGetResponse.Content.ReadAsByteArrayAsync().Result;
    //         var coverImageMat = Cv2.ImDecode(coverImageBytes, ImreadModes.Color);
    //
    //         foreach (var imageUrl in imagesUrl)
    //         {
    //             var otherImageGetResponse = await _httpClient.GetAsync(coverImage);
    //             var otherImageBytes = coverImageGetResponse.Content.ReadAsByteArrayAsync().Result;
    //             var otherImageMat = Cv2.ImDecode(otherImageBytes, ImreadModes.Color);
    //
    //             // Initialize ORB detector
    //             var orb = ORB.Create();
    //
    //             // Detect and compute keypoints and descriptors
    //             var kp1 = new KeyPoint[0];
    //             var kp2 = new KeyPoint[0];
    //             var des1 = new Mat();
    //             var des2 = new Mat();
    //             orb.DetectAndCompute(coverImageMat, null, out kp1, des1);
    //             orb.DetectAndCompute(otherImageMat, null, out kp2, des2);
    //
    //             // Match descriptors using BFMatcher
    //             var bf = new BFMatcher(NormTypes.Hamming, crossCheck: true);
    //             var matches = bf.Match(des1, des2);
    //
    //             // Filter good matches (distance threshold)
    //             var goodMatches = matches.Where(m => m.Distance < 50).ToList();
    //
    //             // Set threshold for matching
    //             if (goodMatches.Count >= 15) // Assuming 15 as the threshold for "similarity"
    //             {
    //                 return true;
    //             }
    //         }
    //
    //         return false;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         return false;
    //     }
    // }
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
    private Dictionary<string, int> CountObjectsInImage(List<DetectResultDto> detectedResults, BoxDto? bookBox)
    {
        var objectCounts = new Dictionary<string, int>();
        if (bookBox is null)
        {
            foreach (var detectResultDto in detectedResults)
            {
                if (objectCounts.ContainsKey(detectResultDto.Name))
                {
                    objectCounts[detectResultDto.Name]++;
                }
                else
                {
                    objectCounts[detectResultDto.Name] = 1;
                }
            }
        }
        else
        {
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