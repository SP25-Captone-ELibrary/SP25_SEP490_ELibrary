using System.Drawing;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Headers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using MultipartFormDataContent = System.Net.Http.MultipartFormDataContent;

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

    public async Task<List<DetectResultDto>> DetectAllAsync(IFormFile image)
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
    public Dictionary<string, int> CountObjectsInImage(List<DetectResultDto> detectedResults, BoxDto? bookBox)
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

    public async Task<IServiceResult> RawDetectAsync(IFormFile image, int id)
    {
        try
        {
            var item = await _libraryItemService.GetByIdAsync(id);
            if (item.Data == null)
            {
                return new ServiceResult(
                    ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002)
                );
            }

            var itemValue = (LibraryItemDto)item.Data!;

            // Tải ảnh từ Cloudinary
            using var cloudinaryResponse = await _httpClient.GetAsync(itemValue.CoverImage);
            if (!cloudinaryResponse.IsSuccessStatusCode)
            {
                return new ServiceResult(
                    ResultCodeConst.AIService_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004)
                );
            }

            await using var cloudinaryStream = await cloudinaryResponse.Content.ReadAsStreamAsync();

            // Gửi request detect objects (cho cả ảnh upload và ảnh từ Cloudinary)
            var detectTasks = new List<Task<DetectResponseDto>>();
            detectTasks.Add(DetectObjectsAsync(image.OpenReadStream(), image.FileName, image.ContentType));
            detectTasks.Add(DetectObjectsAsync(cloudinaryStream, "cloudinary_image.jpg", "image/jpeg"));

            var detectResults = await Task.WhenAll(detectTasks);

            var uploadedImageResults =
                detectResults[0].Images.FirstOrDefault()?.Results ?? new List<DetectResultDto>();

            var cloudinaryImageResults =
                detectResults[1].Images.FirstOrDefault()?.Results ?? new List<DetectResultDto>();
            
            // Chuyển đổi kết quả phát hiện
            var detectedObjects = uploadedImageResults.Select(result => new ObjectInfoDto
            {
                Name = result.Name ?? "Unknown",
                Percentage = result.Confidence * 100
            }).ToList();

            var currentDetectedObjects = cloudinaryImageResults.Select(result => new ObjectInfoDto
            {
                Name = result.Name ?? "Unknown",
                Percentage = result.Confidence * 100
            }).ToList();

            // Chuẩn bị response
            var finalResponse = new RawDetectionResultResponse
            {
                ImportImageDetected = detectedObjects,
                CurrentItemDetected = currentDetectedObjects
            };

            return new ServiceResult(
                ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                finalResponse
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Object detection failed");
            throw new Exception("Error invoke when detecting objects");
        }
    }

    public async Task<IServiceResult> DetectWithEmgu(IFormFile image, string groupCode)
    {
        try
        {
            // Dictionary to save score
            Dictionary<int, double> matchScores = new Dictionary<int, double>();
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            var groupBaseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemGroup!.AiTrainingCode
                .Equals(groupCode));
            groupBaseSpec.EnableSplitQuery();
            groupBaseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemGroup!));
            var itemsInGroup = await _libraryItemService.GetAllWithoutAdvancedSpecAsync(groupBaseSpec);
            if (itemsInGroup.Data is null)
            {
                var errMsg = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),"Items");
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng
                        ? errMsg
                        : "Không tìm thấy bất kỳ tài liệu nào trong nhóm");
            }

            // Create Mat fot user input
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            Mat userInputMat = new Mat();
            CvInvoke.Imdecode(imageBytes,ImreadModes.Grayscale,userInputMat);
            // Create Mat for each cover image
            var itemsInGroupData = (List<LibraryItemDto>)itemsInGroup.Data!;
            
            foreach (var item in itemsInGroupData)
            {
                var response = await _httpClient.GetByteArrayAsync(item.CoverImage);
                // if (!response.IsSuccessStatusCode)
                // {
                //     var errMsg = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002)
                //         ,"cover image");
                //     return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                //         isEng
                //             ? errMsg
                //             : "Không tìm thấy bất kỳ tài liệu nào trong nhóm");
                // }
                using MemoryStream ms = new MemoryStream(response);
                Mat output = new Mat(); 
                CvInvoke.Imdecode(response, ImreadModes.Grayscale,output);
                double score = MatchBookCovers(output, userInputMat);
                matchScores[item.LibraryItemId] = score;
            }
            
            // Normalize scores to a percentage based on the best match
            var responseValue = matchScores.Select(x =>
            {
                var maxScore = matchScores.Values.Max();
                Dictionary<int,double> returnValue = new Dictionary<int,double>();
                foreach (var i in matchScores.Keys)
                {
                    returnValue.Add(i,matchScores[i]/maxScore *100);
                }

                return returnValue;
            }).FirstOrDefault();


            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                responseValue);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Object detection failed");
            throw new Exception("Error invoke when detecting objects with Emgu");
        }
    }
    
    private double MatchBookCovers(Mat img1, Mat img2)
    {
        SIFT sift = new SIFT();
        VectorOfKeyPoint keypoints1 = new VectorOfKeyPoint();
        Mat descriptors1 = new Mat();
        VectorOfKeyPoint keypoints2 = new VectorOfKeyPoint();
        Mat descriptors2 = new Mat();

        sift.DetectAndCompute(img1, null, keypoints1, descriptors1, false);
        sift.DetectAndCompute(img2, null, keypoints2, descriptors2, false);

        if (keypoints1.Size == 0 || keypoints2.Size == 0)
            return 0;  // No keypoints found, return 0 match

        BFMatcher matcher = new BFMatcher(DistanceType.L2);
        VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
        matcher.KnnMatch(descriptors1, descriptors2, matches, 2);

        // Lowe's Ratio Test: Filter out weak matches
        List<MKeyPoint> goodMatches = new List<MKeyPoint>();
        List<MKeyPoint> matchedKeypoints1 = new List<MKeyPoint>();
        List<MKeyPoint> matchedKeypoints2 = new List<MKeyPoint>();

        foreach (var match in matches.ToArrayOfArray())
        {
            if (match.Length > 1 && match[0].Distance < 0.75 * match[1].Distance)
            {
                goodMatches.Add(keypoints1[match[0].QueryIdx]);
                matchedKeypoints1.Add(keypoints1[match[0].QueryIdx]);
                matchedKeypoints2.Add(keypoints2[match[0].TrainIdx]);
            }
        }

        if (goodMatches.Count >= 10)
        {
            Mat homography = GetHomography(matchedKeypoints1, matchedKeypoints2);

            if (!homography.IsEmpty)
            {
                return goodMatches.Count; // Use count as raw score
            }
        }

        return 0; // Return 0 if Homography check fails
    }
    private Mat GetHomography(List<MKeyPoint> keypoints1, List<MKeyPoint> keypoints2)
    {
        if (keypoints1.Count < 4 || keypoints2.Count < 4)
        {
            return new Mat(); // Not enough points for Homography
        }

        PointF[] srcPoints = keypoints1.Select(kp => kp.Point).ToArray();
        PointF[] dstPoints = keypoints2.Select(kp => kp.Point).ToArray();

        Mat homography = CvInvoke.FindHomography(
            srcPoints, dstPoints, RobustEstimationAlgorithm.Ransac, 3);

        return homography;
    }

    private async Task<DetectResponseDto?> DetectObjectsAsync(Stream imageStream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_monitor.DetectModelUrl), "model");
        content.Add(new StringContent(_monitor.DetectImageSize.ToString()), "imgsz");
        content.Add(new StringContent(_monitor.DetectConfidence.ToString()), "conf");
        content.Add(new StringContent(_monitor.DetectIOU.ToString()), "iou");

        var fileContent = new StreamContent(imageStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, _monitor.DetectAPIUrl)
        {
            Content = content
        };
        request.Headers.Add("x-api-key", _monitor.DetectAPIKey);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var stringResponse = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<DetectResponseDto>(stringResponse, new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });
    }
    
    private (float x1, float y1, float x2, float y2) ConvertBoxCoordinates(BoxDto box, int width, int height)
    {
        return (
            (float)(box.X1 * width),
            (float)(box.Y1 * height),
            (float)(box.X2 * width),
            (float)(box.Y2 * height)
        );
    }

    private (byte r, byte g, byte b) GenerateRandomColor(Random random)
    {
        return (
            (byte)random.Next(0, 256),
            (byte)random.Next(0, 256),
            (byte)random.Next(0, 256)
        );
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