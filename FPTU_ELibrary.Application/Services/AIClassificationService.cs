using System.Net.Http.Headers;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using CloudinaryDotNet.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Dtos.AIServices.Speech;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Utils;
using SixLabors.ImageSharp;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Processing;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace FPTU_ELibrary.Application.Services;

public class AIClassificationService : IAIClassificationService
{
    private readonly HttpClient _httpClient;
    private readonly CustomVisionSettings _monitor;
    private readonly string _baseUrl;
    private readonly ILogger _logger;
    private readonly ISystemMessageService _msgService;
    private IHubContext<AiHub> _hubContext;
    private ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly IServiceProvider _service;
    private readonly IAIDetectionService _aiDetectionService;
    private readonly string _basePredictUrl;
    private readonly IOCRService _ocrService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGroupService;

    public AIClassificationService(HttpClient httpClient, ISystemMessageService msgService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        IHubContext<AiHub> hubContext, IAIDetectionService aiDetectionService
        , IOptionsMonitor<CustomVisionSettings> monitor, ILogger logger,
        IServiceProvider service, IOCRService ocrService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGroupService)
    {
        _ocrService = ocrService;
        _libraryItemGroupService = libraryItemGroupService;
        _aiDetectionService = aiDetectionService;
        _hubContext = hubContext;
        _msgService = msgService;
        _httpClient = httpClient;
        _libraryItemService = libraryItemService;
        _monitor = monitor.CurrentValue;
        _logger = logger;
        _service = service;
        _baseUrl = string.Format(_monitor.BaseAIUrl, _monitor.TrainingEndpoint, _monitor.ProjectId);
        _basePredictUrl = string.Format(_monitor.BasePredictUrl, _monitor.PredictionEndpoint, _monitor.ProjectId,
            _monitor.PublishedName);
    }

    public async Task<IServiceResult> TrainModel(Guid trainingCode, List<IFormFile> images, string email)
    {
        // Đọc dữ liệu file vào MemoryStream trước khi chạy background task
        var fileDataList = new List<(byte[] FileBytes, string FileName)>();
        var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(lig
            => lig.AiTrainingCode.Equals(trainingCode));
        groupBaseSpec.ApplyInclude(q => q.Include(
                lig => lig.LibraryItems)
            .ThenInclude(li => li.Category));
        var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
        if (group.Data is null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "group"));
        }
        var groupValue = (LibraryItemGroupDto)group.Data!;
        if (groupValue.LibraryItems.Any(li => li.Category.IsAllowAITraining == false))
        {
            return new ServiceResult(ResultCodeConst.AIService_Warning0005,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0005));
        }

        foreach (var file in images)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            fileDataList.Add((memoryStream.ToArray(), file.FileName)); // Chuyển sang byte[]
        }

        // Chạy background task với dữ liệu đã copy vào bộ nhớ
        var backgroundTask = Task.Run(() => ProcessTrainingTask(trainingCode, fileDataList, email));

        // Trả về kết quả ngay lập tức
        var successMessage = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003);
        var result = new ServiceResult(ResultCodeConst.AIService_Success0003, successMessage);

        _ = backgroundTask; // Bảo đảm task chạy tiếp trong background

        return result;
    }


    public async Task ProcessTrainingTask(Guid trainingCode, List<(byte[] FileBytes, string FileName)> images,
        string email)
    {
        // define services that use in background task
        using var scope = _service.CreateScope();
        var libraryItemService = scope.ServiceProvider.GetRequiredService<ILibraryItemService<LibraryItemDto>>();
        var libraryItemGroupService =
            scope.ServiceProvider.GetRequiredService<ILibraryItemGroupService<LibraryItemGroupDto>>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AiHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
        var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CustomVisionSettings>>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        // define monitor value
        var currentAiConfiguration = monitor.CurrentValue;
        try
        {
            var memoryStreams = new List<(MemoryStream Stream, string FileName)>();
            foreach (var (fileBytes, fileName) in images)
            {
                var memoryStream = new MemoryStream(fileBytes);
                memoryStreams.Add((memoryStream, fileName));
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
            List<TagDto> tags = await GetTagAsync(baseConfig);
            TagDto tag;
            if (!tags.Select(x => x.Name).ToList().Contains(trainingCode.ToString()!))
            {
                tag = await CreateTagAsync(baseConfig, trainingCode);
            }
            else
            {
                tag = tags.FirstOrDefault(x => x.Name.Equals(trainingCode.ToString()));
            }

            //Get group and item in group
            var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(lig
                => lig.AiTrainingCode.Equals(trainingCode.ToString()));
            groupBaseSpec.ApplyInclude(q =>
                q.Include(lig => lig.LibraryItems));
            var group = await libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
            if (group.Data is null)
            {
                await hubContext.Clients.User(email).SendAsync("Fail when get group");
                return;
            }

            var groupValue = (LibraryItemGroupDto)group.Data!;
            var libraryItemIds = groupValue.LibraryItems.Select(lib => lib.LibraryItemId).ToList();
            foreach (var groupValueLibraryItem in groupValue.LibraryItems
                         .Where(li => li.IsTrained == false)
                         .ToList()
                    )
            {
                var response = await httpClient.GetAsync(groupValueLibraryItem.CoverImage);
                // using response.IsSuccessStatusCode to check if the request is successful
                if (!response.IsSuccessStatusCode)
                {
                    await hubContext.Clients.User(email).SendAsync("Get cover image unsuccessfully");
                    return;
                }

                var memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                memoryStream.Position = 0;
                memoryStreams.Add((memoryStream, $"{groupValueLibraryItem.LibraryItemId}_cover.jpg"));
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
            await libraryItemService.UpdateTrainingStatusAsync(libraryItemIds);
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

    public async Task<IServiceResult> GetAvailableGroup(string email, int rootItemId, List<int>? otherItemIds)
    {
        try
        {
            ItemGroupForAIDto response = new ItemGroupForAIDto()
            {
                NewLibraryIdsToTrain = otherItemIds ?? new List<int>()
            };
            // Find suitable group for items
            var suitableGroupId = await SuitableLibraryGroup(rootItemId);
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == rootItemId);
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            // Get root item
            var rootItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var rootItemValue = (LibraryItemDto)rootItem.Data!;
            if (rootItemValue.GroupId != null)
                return new ServiceResult(ResultCodeConst.AIService_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0006));
            // Case suitable group is found
            if (suitableGroupId != 0)
            {
                var suitableGroup = await _libraryItemGroupService.GetByIdAsync(suitableGroupId);
                if (suitableGroup.Data is null)
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                            "group"));
                var suitableGroupValue = (LibraryItemGroupDto)suitableGroup.Data!;
                // Case select many items
                if (otherItemIds != null)
                {
                    foreach (var otherItemId in otherItemIds)
                    {
                        var otherItem = await _libraryItemService.GetByIdAsync(otherItemId);
                        var otherItemValue = (LibraryItemDto)otherItem.Data!;
                        if (otherItemValue.GroupId != null)
                        {
                            return new ServiceResult(ResultCodeConst.AIService_Warning0006,
                                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0006));
                        }
                    }

                    var updateResponse = await _libraryItemService.UpdateGroupIdAsync(otherItemIds, suitableGroupId);
                    if (updateResponse.ResultCode != ResultCodeConst.SYS_Success0003)
                    {
                        return updateResponse;
                    }
                }
                else
                {
                    var updateResponse =
                        await _libraryItemService.UpdateGroupIdAsync(new List<int>() { rootItemId }, suitableGroupId);
                    if (updateResponse.ResultCode != ResultCodeConst.SYS_Success0003)
                    {
                        return updateResponse;
                    }
                }

                // Return suitable GroupId
                response.TrainingCode = Guid.Parse(suitableGroupValue.AiTrainingCode);
            }
            else
            {
                // Create new Group based on root item
                var mainAuthor =
                    rootItemValue.LibraryItemAuthors.First(x => x.LibraryItemId == rootItemId)!
                        .Author.FullName;
                var newGroupDto = new LibraryItemGroupDto()
                {
                    Author = mainAuthor,
                    Title = rootItemValue.Title,
                    SubTitle = rootItemValue.SubTitle,
                    CutterNumber = rootItemValue.CutterNumber,
                    ClassificationNumber = rootItemValue.ClassificationNumber,
                    CreatedAt = DateTime.Now,
                    CreatedBy = email,
                    TopicalTerms = rootItemValue.TopicalTerms,
                    AiTrainingCode = Guid.NewGuid().ToString(),
                };
                var createGroupResponse = await _libraryItemGroupService.CreateAsync(newGroupDto);
                if (createGroupResponse.ResultCode != ResultCodeConst.SYS_Success0001)
                {
                    return createGroupResponse;
                }

                response.TrainingCode = Guid.Parse(newGroupDto.AiTrainingCode);
                var newGroup = await _libraryItemGroupService.GetWithSpecAsync(new BaseSpecification<LibraryItemGroup>(
                    lig => lig.AiTrainingCode.Equals(newGroupDto.AiTrainingCode)));
                var newGroupValue = (LibraryItemGroupDto)newGroup.Data!;
                if (otherItemIds != null)
                {
                    var updateOtherItemResponse =
                        await _libraryItemService.UpdateGroupIdAsync(otherItemIds, newGroupValue.GroupId);
                    if (updateOtherItemResponse.ResultCode != ResultCodeConst.SYS_Success0003)
                    {
                        return updateOtherItemResponse;
                    }
                }

                var updateResponse =
                    await _libraryItemService.UpdateGroupIdAsync(new List<int>() { rootItemId },
                        newGroupValue.GroupId);
                if (updateResponse.ResultCode != ResultCodeConst.SYS_Success0003)
                {
                    return updateResponse;
                }
            }

            response.NewLibraryIdsToTrain.Add(rootItemId);
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoking Get Available Group");
        }
    }


    // create a function with List<int> libraryItemIds as parameter and check if their field could be able to be in a group or not
    // base on CutterNumber,ClassificationNumber,mainAuthor,Title

    #region IsAbleToCreateGroup func

    public async Task<IServiceResult> IsAbleToCreateGroup(int rootItemId, List<int>? otherItemIds)
    {
        try
        {
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == rootItemId);
            var listPropertiesChecked = new List<CheckedGroupDetailDto<string>>();
            var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            var libraryItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var libraryItemValue = (LibraryItemDto)libraryItem.Data!;
            var mainAuthor = libraryItemValue.LibraryItemAuthors
                .First(x => x.LibraryItemId == rootItemId)!.Author.FullName;

            listPropertiesChecked.Add(new CheckedGroupDetailDto<string>()
            {
                PropertiesChecked = new Dictionary<string, int>()
                {
                    { nameof(libraryItemValue.CutterNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(libraryItemValue.ClassificationNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(Author), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(libraryItemValue.Title), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(libraryItemValue.SubTitle), (int)FieldGroupCheckedStatus.GroupSuccess }
                },
                ItemId = libraryItemValue.LibraryItemId,
                IsRoot = true
            });

            if (otherItemIds is not null)
            {
                foreach (var otherItemId in otherItemIds)
                {
                    var itemCheckedResult = new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                    };

                    var candidateSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == otherItemId);
                    candidateSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                        .ThenInclude(lia => lia.Author));
                    var candidateItem = await _libraryItemService.GetWithSpecAsync(candidateSpec);
                    var candidateItemValue = (LibraryItemDto)candidateItem.Data!;
                    var mainAuthorOfCandidate = candidateItemValue.LibraryItemAuthors
                        .First(x => x.LibraryItemId == otherItemId)!.Author.FullName;

                    itemCheckedResult.PropertiesChecked.Add(
                        nameof(libraryItemValue.CutterNumber),
                        libraryItemValue.CutterNumber == candidateItemValue.CutterNumber
                            ? (int)FieldGroupCheckedStatus.GroupSuccess
                            : (int)FieldGroupCheckedStatus.GroupFailed);

                    itemCheckedResult.PropertiesChecked.Add(
                        nameof(libraryItemValue.ClassificationNumber),
                        libraryItemValue.ClassificationNumber == candidateItemValue.ClassificationNumber
                            ? (int)FieldGroupCheckedStatus.GroupSuccess
                            : (int)FieldGroupCheckedStatus.GroupFailed);

                    itemCheckedResult.PropertiesChecked.Add(
                        nameof(Author),
                        mainAuthor == mainAuthorOfCandidate
                            ? (int)FieldGroupCheckedStatus.GroupSuccess
                            : (int)FieldGroupCheckedStatus.GroupFailed);

                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(candidateItemValue.Title),
                        StringUtils.RemoveSpecialCharacter(libraryItemValue.Title));

                    int subTitleStatus;
                    if (string.IsNullOrEmpty(candidateItemValue.SubTitle) &&
                        string.IsNullOrEmpty(libraryItemValue.SubTitle))
                    {
                        subTitleStatus = titleStatus;
                    }
                    else
                    {
                        subTitleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(candidateItemValue.SubTitle ?? ""),
                            StringUtils.RemoveSpecialCharacter(libraryItemValue.SubTitle ?? ""));
                    }

                    var isSubTitleNull = string.IsNullOrEmpty(libraryItemValue.SubTitle);
                    var titleSubTitleStatus = CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);

                    itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                    determineOverallStatus.Add(itemCheckedResult);
                    itemCheckedResult.PropertiesChecked.Add(nameof(libraryItemValue.Title), titleStatus);
                    itemCheckedResult.PropertiesChecked.Add(nameof(libraryItemValue.SubTitle), subTitleStatus);
                    itemCheckedResult.ItemId = candidateItemValue.LibraryItemId;
                    itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                    listPropertiesChecked.Add(itemCheckedResult);
                }
            }

            var overallStatus = DetermineOverallStatus(determineOverallStatus);

            var responseData = new CheckedGroupResponseDto<string>()
            {
                IsAbleToCreateGroup = overallStatus,
                ListCheckedGroupDetail = listPropertiesChecked
            };

            return new ServiceResult(
                ResultCodeConst.AIService_Success0005,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0005),
                responseData);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Check if item are able to be grouped");
        }
    }

    private int CompareFieldStatus(string field1, string field2)
    {
        var fuzzyScore = StringUtils.CombinedFuzzinessScore(field1, field2);
        return fuzzyScore switch
        {
            >= 90 => (int)FieldGroupCheckedStatus.GroupSuccess,
            >= 50 => (int)FieldGroupCheckedStatus.AbleToForceGrouped,
            _ => (int)FieldGroupCheckedStatus.GroupFailed
        };
    }

    private int CombineTitleSubTitleStatus(int titleStatus, int subTitleStatus, bool isSubTitleNull)
    {
        if (isSubTitleNull)
        {
            return (titleStatus, subTitleStatus) switch
            {
                ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
                    FieldGroupCheckedStatus.GroupFailed),
                ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
                    FieldGroupCheckedStatus.GroupSuccess),
                ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
                    FieldGroupCheckedStatus.GroupFailed),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupFailed)) =>
                    (int)(FieldGroupCheckedStatus.GroupFailed),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.AbleToForceGrouped))
                    => (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupSuccess)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
                    FieldGroupCheckedStatus.GroupSuccess),
                _ => (int)(FieldGroupCheckedStatus.GroupFailed)
            };
        }
        else
        {
            return (titleStatus, subTitleStatus) switch
            {
                ((int)FieldGroupCheckedStatus.GroupFailed, (int)FieldGroupCheckedStatus.GroupFailed) => (int)
                    FieldGroupCheckedStatus.GroupFailed,
                ((int)FieldGroupCheckedStatus.GroupFailed, (int)FieldGroupCheckedStatus.GroupSuccess) => (int)
                    FieldGroupCheckedStatus.GroupSuccess,
                ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupFailed)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
                    FieldGroupCheckedStatus.GroupSuccess),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.AbleToForceGrouped))
                    => (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupSuccess)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                    (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
                ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
                    FieldGroupCheckedStatus.GroupSuccess),
                _ => (int)(FieldGroupCheckedStatus.GroupFailed)
            };
        }
    }

    private int DetermineOverallStatus(List<CheckedGroupDetailDto<string>> listPropertiesChecked)
    {
        bool hasFail = listPropertiesChecked.Any(x =>
            x.PropertiesChecked.Values.Contains((int)FieldGroupCheckedStatus.GroupFailed));
        bool hasForce = listPropertiesChecked.Any(x =>
            x.PropertiesChecked.Values.Contains((int)FieldGroupCheckedStatus.AbleToForceGrouped));

        if (hasFail) return (int)FieldGroupCheckedStatus.GroupFailed;
        if (hasForce) return (int)FieldGroupCheckedStatus.AbleToForceGrouped;
        return (int)FieldGroupCheckedStatus.GroupSuccess;
    }

    #endregion

    private async Task<int> SuitableLibraryGroup(int currentLibraryItemId)
    {
        try
        {
            // Lấy thông tin LibraryItem hiện tại
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == currentLibraryItemId);
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            var libraryItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var libraryItemValue = (LibraryItemDto)libraryItem.Data!;
            var mainAuthor = libraryItemValue.LibraryItemAuthors.First(x => x.LibraryItemId == currentLibraryItemId)!
                .Author.FullName;

            // Lấy các nhóm có tiềm năng dựa vào CutterNumber, ClassificationNumber, Author
            var groupSpec = new BaseSpecification<LibraryItemGroup>(lig =>
                lig.CutterNumber == libraryItemValue.CutterNumber &&
                lig.ClassificationNumber == libraryItemValue.ClassificationNumber &&
                lig.Author.Equals(mainAuthor));

            var potentialGroups = await _libraryItemGroupService.GetAllWithSpecAsync(groupSpec);

            if (potentialGroups.Data is null || !((List<LibraryItemGroupDto>)potentialGroups.Data).Any())
                return 0; // Không tìm thấy nhóm phù hợp

            var groupList = (List<LibraryItemGroupDto>)potentialGroups.Data!;

            // Tính điểm cho từng nhóm tiềm năng
            int bestGroupId = 0;
            double bestScore = 0;

            foreach (var group in groupList)
            {
                // Tính điểm cho Title và SubTitle
                var titleScore = StringUtils.CombinedFuzzinessScore(libraryItemValue.Title, group.Title);
                var subTitleScore = libraryItemValue.SubTitle != null && group.SubTitle != null
                    ? StringUtils.CombinedFuzzinessScore(libraryItemValue.SubTitle, group.SubTitle)
                    : 0;

                // Xác định trạng thái cho Title và SubTitle
                var titleStatus = CompareFieldStatus(libraryItemValue.Title, group.Title);
                int subTitleStatus;
                if (string.IsNullOrEmpty(libraryItemValue.SubTitle) &&
                    string.IsNullOrEmpty(group.SubTitle))
                {
                    subTitleStatus = titleStatus;
                }
                else
                {
                    subTitleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(libraryItemValue.SubTitle ?? ""),
                        StringUtils.RemoveSpecialCharacter(group.SubTitle ?? ""));
                }

                // Kết hợp điểm của Title và SubTitle
                var combinedStatus = CombineTitleSubTitleStatus(titleStatus, subTitleStatus, group.SubTitle == null);

                // Gán điểm cho nhóm dựa vào trạng thái kết hợp
                double groupScore = combinedStatus switch
                {
                    (int)FieldGroupCheckedStatus.GroupSuccess => 100, // Điểm cao nhất
                    (int)FieldGroupCheckedStatus.AbleToForceGrouped => 50, // Điểm trung bình
                    _ => 0 // Điểm thấp nếu không phù hợp
                };

                // Kiểm tra nếu nhóm này có điểm cao nhất
                if (groupScore > bestScore)
                {
                    bestScore = groupScore;
                    bestGroupId = group.GroupId;
                }
            }

            return bestGroupId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error while finding suitable group.");
        }
    }


    
    // public async Task<IServiceResult> PredictAsync(IFormFile image)
    // {
    //     try
    //     {
    //         // Detect bounding boxes for books
    //         var bookBoxes = await _aiDetectionService.DetectAsync(image);
    //
    //         if (!bookBoxes.Any())
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0003,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
    //         }
    //
    //         // Crop images based on bounding boxes
    //         using (var imageStream = image.OpenReadStream())
    //         using (var ms = new MemoryStream())
    //         {
    //             await imageStream.CopyToAsync(ms);
    //             var imageBytes = ms.ToArray();
    //             var croppedImages = CropImages(imageBytes, bookBoxes);
    //             List<Guid> bookCodes = new List<Guid>();
    //             var predictResponse = new PredictionResponseDto()
    //             {
    //                 NumberOfBookDetected = croppedImages.Count,
    //                 LibraryItemPrediction = new List<PossibleLibraryItem>()
    //             };
    //             // detect and predict base on cropped images
    //             foreach (var croppedImage in croppedImages)
    //             {
    //                 var content = new ByteArrayContent(croppedImage);
    //                 content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    //                 _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
    //                 var response = await _httpClient.PostAsync(_basePredictUrl, content);
    //                 response.EnsureSuccessStatusCode();
    //                 var jsonResponse = await response.Content.ReadAsStringAsync();
    //                 var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
    //                     new JsonSerializerOptions
    //                     {
    //                         PropertyNameCaseInsensitive = true,
    //                     });
    //                 var bestPrediction =
    //                     predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
    //                 var baseSpec = new BaseSpecification<Book>(x =>
    //                     x.BookCodeForAITraining.ToString().ToLower().Equals(bestPrediction.TagName));
    //                 baseSpec.ApplyInclude(q =>
    //                     q.Include(x => x.BookEditions)
    //                         .ThenInclude(be => be.BookEditionAuthors)
    //                         .ThenInclude(ea => ea.Author));
    //                 var bookSearchResult = await _bookService.GetWithSpecAsync(baseSpec);
    //                 if (bookSearchResult.ResultCode != ResultCodeConst.SYS_Success0002)
    //                 {
    //                     return bookSearchResult;
    //                 }
    //
    //                 var book = (BookDto)bookSearchResult.Data!;
    //                 var availableEdition = new List<LibraryItemDto>();
    //
    //                 foreach (var edition in book.BookEditions)
    //                 {
    //                     var stream = new MemoryStream(croppedImage);
    //                     var bookInfo = new CheckedBookEditionDto()
    //                     {
    //                         Title = edition.EditionTitle,
    //                         Authors = edition.BookEditionAuthors.Where(x => x.BookEditionId == edition.BookEditionId)
    //                             .Select(x => x.Author.FullName).ToList(),
    //                         Publisher = edition.Publisher ?? " ",
    //                         Image = new FormFile(stream, 0, stream.Length, "file", edition.EditionTitle
    //                             + edition.EditionNumber)
    //                         {
    //                             Headers = new HeaderDictionary(),
    //                             ContentType = "application/octet-stream"
    //                         }
    //                     };
    //                     var compareResult = await _ocrService.CheckBookInformationAsync(bookInfo);
    //                     var compareResultValue = (MatchResultDto)compareResult.Data!;
    //                     if (compareResultValue.TotalPoint > compareResultValue.ConfidenceThreshold)
    //                     {
    //                         availableEdition.Add(edition);
    //                     }
    //                 }
    //
    //                 //add available edition to response
    //                 predictResponse.LibraryItemPrediction.Add(new PossibleLibraryItem()
    //                 {
    //                     LibraryItemDetails = availableEdition,
    //                     BookCode = book.BookCodeForAITraining.ToString()
    //                 });
    //             }
    //
    //             return new ServiceResult(ResultCodeConst.AIService_Success0003,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003), predictResponse
    //             );
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when Predict Book Model");
    //     }
    // }
    
    // public async Task<IServiceResult> Recommendation(IFormFile image)
    // {
    //     try
    //     {
    //         // detect bounding boxes for books and check if it is more than 2 books
    //         var bookBoxes = await _aiDetectionService.DetectAsync(image);
    //         if (!bookBoxes.Any())
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0003,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
    //         }
    //
    //         if (bookBoxes.Count > 1)
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0004,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
    //         }
    //
    //         //crop image to get the picture of the book only
    //         using (var imageStream = image.OpenReadStream())
    //         using (var ms = new MemoryStream())
    //         {
    //             await imageStream.CopyToAsync(ms);
    //             var imageBytes = ms.ToArray();
    //             var croppedImage = CropImages(imageBytes, bookBoxes).First();
    //
    //             //using the cropped image to predict book
    //             var content = new ByteArrayContent(croppedImage);
    //             content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    //             _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
    //             var response = await _httpClient.PostAsync(_basePredictUrl, content);
    //             response.EnsureSuccessStatusCode();
    //             var jsonResponse = await response.Content.ReadAsStringAsync();
    //             var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
    //                 new JsonSerializerOptions
    //                 {
    //                     PropertyNameCaseInsensitive = true,
    //                 });
    //             var bestPrediction =
    //                 predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
    //             var baseSpec = new BaseSpecification<Book>(x =>
    //                 x.BookCodeForAITraining.ToString().ToLower().Equals(bestPrediction.TagName));
    //             baseSpec.ApplyInclude(q =>
    //                 q.Include(x => x.BookEditions)
    //                     .ThenInclude(be => be.BookEditionAuthors)
    //                     .ThenInclude(ea => ea.Author));
    //             var bookSearchResult = await _bookService.GetWithSpecAsync(baseSpec);
    //             if (bookSearchResult.ResultCode != ResultCodeConst.SYS_Success0002)
    //             {
    //                 return bookSearchResult;
    //             }
    //
    //             var book = (BookDto)bookSearchResult.Data!;
    //             var relatedBookEdition = new List<RelatedItemDto<LibraryItemDto>>();
    //             //check the exact edition in the book
    //             foreach (var edition in book.BookEditions)
    //             {
    //                 var stream = new MemoryStream(croppedImage);
    //                 var bookInfo = new CheckedBookEditionDto()
    //                 {
    //                     Title = edition.EditionTitle,
    //                     Authors = edition.BookEditionAuthors.Where(x => x.BookEditionId == edition.BookEditionId)
    //                         .Select(x => x.Author.FullName).ToList(),
    //                     Publisher = edition.Publisher ?? " ",
    //                     Image = new FormFile(stream, 0, stream.Length, "file", edition.EditionTitle
    //                                                                            + edition.EditionNumber)
    //                     {
    //                         Headers = new HeaderDictionary(),
    //                         ContentType = "application/octet-stream"
    //                     }
    //                 };
    //
    //                 var compareResult = await _ocrService.CheckBookInformationAsync(bookInfo);
    //                 var compareResultValue = (MatchResultDto)compareResult.Data!;
    //                 if (compareResultValue.TotalPoint > compareResultValue.ConfidenceThreshold)
    //                 {
    //                     var sameAuthorEditions =
    //                         await _libraryItemService.GetRelatedEditionWithMatchFieldAsync(edition, nameof(Author));
    //                     relatedBookEdition.Add(new RelatedItemDto<LibraryItemDto>()
    //                     {
    //                         RelatedProperty = nameof(Author),
    //                         RelatedItems = sameAuthorEditions.Data as List<LibraryItemDto>
    //                     });
    //
    //                    var sameCategoryEditions = await _libraryItemService
    //                        .GetRelatedEditionWithMatchFieldAsync(edition, nameof(Category));
    //                     relatedBookEdition.Add(new RelatedItemDto<LibraryItemDto>()
    //                     {
    //                         RelatedItems = sameCategoryEditions.Data as List<LibraryItemDto>,
    //                         RelatedProperty = nameof(Category)
    //                     });
    //                 }
    //             }
    //
    //             return new ServiceResult(ResultCodeConst.SYS_Success0002,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
    //                 relatedBookEdition);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when giving recommendation Book Model");
    //     }
    // }
    
    
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
                await response.Content.ReadAsStringAsync();
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

    private async Task PublishIterationAsync(BaseConfigurationBackgroudDto dto, Guid iterationId,
        string publishName)
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