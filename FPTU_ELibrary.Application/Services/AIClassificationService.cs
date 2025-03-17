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
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Utils;
using SixLabors.ImageSharp;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications;
using iTextSharp.text;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
    private readonly IAITrainingSessionService<AITrainingSessionDto> _aiTrainingSessionService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGroupService;

    public AIClassificationService(HttpClient httpClient, ISystemMessageService msgService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        IHubContext<AiHub> hubContext, IAIDetectionService aiDetectionService
        , IOptionsMonitor<CustomVisionSettings> monitor, ILogger logger,
        IServiceProvider service, IOCRService ocrService,
        IAITrainingSessionService<AITrainingSessionDto> aiTrainingSessionService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGroupService)
    {
        _ocrService = ocrService;
        _aiTrainingSessionService = aiTrainingSessionService;
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
            => lig.AiTrainingCode.Equals(trainingCode.ToString()));
        groupBaseSpec.ApplyInclude(q => q.Include(
                lig => lig.LibraryItems)
            .ThenInclude(li => li.Category));
        var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec, false);
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

            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 10, groupCode = tag }
            );
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
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 20, groupCode = tag }
            );
            // Train the model after adding the images
            var iteration = await TrainProjectAsync(baseConfig);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 30, groupCode = tag }
            );
            if (iteration is null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            }

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 40, groupCode = tag }
            );
            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 50, groupCode = tag }
            );
            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(baseConfig, iteration.Id, monitor.CurrentValue.PublishedName);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 60, groupCode = tag }
            );
            await libraryItemService.UpdateTrainingStatusAsync(libraryItemIds);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 80, groupCode = tag }
            );
            //Send notification when finish
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 100, groupCode = tag }
            );
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
                .ThenInclude(lia => lia.Author)
                .Include(li => li.LibraryItemGroup)!);
            // Get root item
            var rootItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var rootItemValue = (LibraryItemDto)rootItem.Data!;
            if (rootItemValue.GroupId != null)
            {
                response.TrainingCode = Guid.Parse(rootItemValue.LibraryItemGroup!.AiTrainingCode);
                response.NewLibraryIdsToTrain.Add(rootItemId);
                if (otherItemIds != null) response.NewLibraryIdsToTrain.AddRange(otherItemIds);
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    response);
            }

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
                if (otherItemIds != null && otherItemIds!.Count != 0)
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

    public async Task<IServiceResult> GetAndGradeAllSuitableItemsForGrouping(int rootItemId)
    {
        try
        {
            // Get selected item
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == rootItemId);
            baseSpec.EnableSplitQuery();
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            var libraryItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var rootLibraryItemValue = (LibraryItemDto)libraryItem.Data!;
            var mainAuthor = rootLibraryItemValue.LibraryItemAuthors
                .First(x => x.LibraryItemId == rootItemId)!.Author.FullName;
            // Create list of checked properties compare to root item
            var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
            var listPropertiesChecked = new List<CheckedGroupDetailDto<string>>();
            listPropertiesChecked.Add(new CheckedGroupDetailDto<string>()
            {
                PropertiesChecked = new Dictionary<string, int>()
                {
                    { nameof(rootLibraryItemValue.CutterNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(rootLibraryItemValue.ClassificationNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(Author), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(rootLibraryItemValue.Title), (int)FieldGroupCheckedStatus.GroupSuccess },
                    { nameof(rootLibraryItemValue.SubTitle), (int)FieldGroupCheckedStatus.GroupSuccess }
                },
                Item = rootLibraryItemValue,
                IsRoot = true
            });


            var candidateItemsSpec = new BaseSpecification<LibraryItem>
            (li => li.CutterNumber!.Equals(rootLibraryItemValue.CutterNumber) &&
                   li.ClassificationNumber!.Equals(rootLibraryItemValue.ClassificationNumber)
                   && li.LibraryItemAuthors.Any(lia => lia.Author.FullName.Equals(mainAuthor))
                   && li.LibraryItemId != rootItemId
            );

            candidateItemsSpec.EnableSplitQuery();
            candidateItemsSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            var candidateItems = await _libraryItemService.GetAllWithSpecAndWithOutFilterAsync(candidateItemsSpec);
            if (candidateItems.Data is null)
            {
                // only root item in the group
                var responseData = new CheckedGroupResponseDto<string>()
                {
                    IsAbleToCreateGroup = (int)FieldGroupCheckedStatus.GroupSuccess,
                    ListCheckedGroupDetail = listPropertiesChecked
                };
                return new ServiceResult(ResultCodeConst.AIService_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0005), responseData);
            }
            else
            {
                var candidateItemsValue = (List<LibraryItemDto>)candidateItems.Data!;

                foreach (var libraryItemDto in candidateItemsValue)
                {
                    var itemCheckedResult = new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                    };
                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(libraryItemDto.Title),
                        StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.Title));

                    int subTitleStatus;
                    if (rootLibraryItemValue.SubTitle is null && libraryItemDto.SubTitle is null)
                    {
                        subTitleStatus = titleStatus;
                    }
                    else if (rootLibraryItemValue.SubTitle is null && libraryItemDto.SubTitle != null)
                    {
                        subTitleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
                            StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.Title));
                    }
                    else if (rootLibraryItemValue.SubTitle != null && libraryItemDto.SubTitle != null)
                    {
                        subTitleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
                            StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.SubTitle ?? ""));
                    }
                    else
                    {
                        subTitleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.SubTitle ?? ""),
                            StringUtils.RemoveSpecialCharacter(libraryItemDto.Title));
                    }

                    var isSubTitleNull = string.IsNullOrEmpty(rootLibraryItemValue.SubTitle);

                    var titleSubTitleStatus = CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);

                    itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.CutterNumber)
                        , (int)FieldGroupCheckedStatus.GroupSuccess);
                    itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.ClassificationNumber)
                        , (int)FieldGroupCheckedStatus.GroupSuccess);
                    itemCheckedResult.PropertiesChecked.Add(nameof(Author)
                        , (int)FieldGroupCheckedStatus.GroupSuccess);
                    itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                    determineOverallStatus.Add(itemCheckedResult);
                    itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.Title), titleStatus);
                    itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.SubTitle), subTitleStatus);
                    itemCheckedResult.Item = libraryItemDto;
                    itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                    listPropertiesChecked.Add(itemCheckedResult);
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
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            throw new Exception("Error invoke when Get and Grade All Suitable Items For Grouping");
        }
    }

    public async Task<IServiceResult> GetAndGradeAllSuitableItemsForGrouping()
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            var response = new List<CheckedGroupResponseDto<string>>();
            var processedItemIds = new HashSet<int>();
            // Check if is there any training session or not

            var baseSession =
                new BaseSpecification<AITrainingSession>(ts => ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
            baseSession.EnableSplitQuery();
            baseSession.ApplyInclude(q => q.Include(s => s.TrainingDetails));
            var session = await _aiTrainingSessionService.GetWithSpecAsync(baseSession);
            AITrainingSessionDto? sessionValue = null;
            if (session.Data != null)
            {
                sessionValue = (AITrainingSessionDto)session.Data!;
            }

            var isTrainingLibraryItems = sessionValue != null
                ? sessionValue.TrainingDetails.Select(td => td.LibraryItemId).ToList()
                                         :new List<int>();
                
            // foreach (var selectedItemId in selectedItemIds)
            // {
            //     if (processedItemIds.Contains(selectedItemId))
            //         continue; // Bỏ qua nếu item đã được xử lý
            //
            //     processedItemIds.Add(selectedItemId);
            //
            //     // Get selected item
            //     var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == selectedItemId);
            //     baseSpec.EnableSplitQuery();
            //     baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
            //         .ThenInclude(lia => lia.Author));
            //     var libraryItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            //     var rootLibraryItemValue = (LibraryItemDto)libraryItem.Data!;
            //     var mainAuthor = rootLibraryItemValue.LibraryItemAuthors
            //         .First(x => x.LibraryItemId == selectedItemId)!.Author.FullName;
            //
            //     // Create list of checked properties compare to root item
            //     var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
            //     var listPropertiesChecked = new List<CheckedGroupDetailDto<string>>
            //     {
            //         new CheckedGroupDetailDto<string>()
            //         {
            //             PropertiesChecked = new Dictionary<string, int>()
            //             {
            //                 { nameof(rootLibraryItemValue.CutterNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
            //                 {
            //                     nameof(rootLibraryItemValue.ClassificationNumber),
            //                     (int)FieldGroupCheckedStatus.GroupSuccess
            //                 },
            //                 { nameof(Author), (int)FieldGroupCheckedStatus.GroupSuccess },
            //                 { nameof(rootLibraryItemValue.Title), (int)FieldGroupCheckedStatus.GroupSuccess },
            //                 { nameof(rootLibraryItemValue.SubTitle), (int)FieldGroupCheckedStatus.GroupSuccess }
            //             },
            //             Item = rootLibraryItemValue,
            //             IsRoot = true
            //         }
            //     };
            //
            //     var candidateItemsSpec = new BaseSpecification<LibraryItem>(
            //         li => li.CutterNumber!.Equals(rootLibraryItemValue.CutterNumber) &&
            //               li.ClassificationNumber!.Equals(rootLibraryItemValue.ClassificationNumber) &&
            //               li.LibraryItemAuthors.Any(lia => lia.Author.FullName.Equals(mainAuthor)) &&
            //               li.LibraryItemId != selectedItemId
            //     );
            //
            //     candidateItemsSpec.EnableSplitQuery();
            //     candidateItemsSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
            //         .ThenInclude(lia => lia.Author));
            //     var candidateItems = await _libraryItemService.GetAllWithSpecAndWithOutFilterAsync(candidateItemsSpec);
            //
            //     if (candidateItems.Data is null)
            //     {
            //         // only root item in the group
            //         var responseData = new CheckedGroupResponseDto<string>()
            //         {
            //             IsAbleToCreateGroup = (int)FieldGroupCheckedStatus.GroupSuccess,
            //             ListCheckedGroupDetail = listPropertiesChecked
            //         };
            //         return new ServiceResult(ResultCodeConst.AIService_Success0005,
            //             await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0005), responseData);
            //     }
            //     else
            //     {
            //         var candidateItemsValue = (List<LibraryItemDto>)candidateItems.Data!;
            //
            //         // Loại bỏ các item đã được xử lý trước đó
            //         candidateItemsValue.RemoveAll(item => processedItemIds.Contains(item.LibraryItemId));
            //
            //         foreach (var libraryItemDto in candidateItemsValue)
            //         {
            //             var itemCheckedResult = new CheckedGroupDetailDto<string>()
            //             {
            //                 PropertiesChecked = new Dictionary<string, int>()
            //             };
            //
            //             var titleStatus = CompareFieldStatus(
            //                 StringUtils.RemoveSpecialCharacter(libraryItemDto.Title),
            //                 StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.Title));
            //
            //             int subTitleStatus;
            //             if (rootLibraryItemValue.SubTitle is null && libraryItemDto.SubTitle is null)
            //             {
            //                 subTitleStatus = titleStatus;
            //             }
            //             else if (rootLibraryItemValue.SubTitle is null && libraryItemDto.SubTitle != null)
            //             {
            //                 subTitleStatus = CompareFieldStatus(
            //                     StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
            //                     StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.Title));
            //             }
            //             else if (rootLibraryItemValue.SubTitle != null && libraryItemDto.SubTitle != null)
            //             {
            //                 subTitleStatus = CompareFieldStatus(
            //                     StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
            //                     StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.SubTitle ?? ""));
            //             }
            //             else
            //             {
            //                 subTitleStatus = CompareFieldStatus(
            //                     StringUtils.RemoveSpecialCharacter(rootLibraryItemValue.SubTitle ?? ""),
            //                     StringUtils.RemoveSpecialCharacter(libraryItemDto.Title));
            //             }
            //
            //             var isSubTitleNull = string.IsNullOrEmpty(rootLibraryItemValue.SubTitle);
            //
            //             var titleSubTitleStatus =
            //                 CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
            //
            //             itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.CutterNumber)
            //                 , (int)FieldGroupCheckedStatus.GroupSuccess);
            //             itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.ClassificationNumber)
            //                 , (int)FieldGroupCheckedStatus.GroupSuccess);
            //             itemCheckedResult.PropertiesChecked.Add(nameof(Author)
            //                 , (int)FieldGroupCheckedStatus.GroupSuccess);
            //             itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
            //             determineOverallStatus.Add(itemCheckedResult);
            //             itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.Title), titleStatus);
            //             itemCheckedResult.PropertiesChecked.Add(nameof(rootLibraryItemValue.SubTitle), subTitleStatus);
            //             itemCheckedResult.Item = libraryItemDto;
            //             itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
            //             listPropertiesChecked.Add(itemCheckedResult);
            //         }
            //
            //         var overallStatus = DetermineOverallStatus(determineOverallStatus);
            //
            //         var responseData = new CheckedGroupResponseDto<string>()
            //         {
            //             IsAbleToCreateGroup = overallStatus,
            //             ListCheckedGroupDetail = listPropertiesChecked
            //         };
            //         response.Add(responseData);
            //     }
            // }

            var ungroupedItemSpec = new BaseSpecification<LibraryItem>(li => !li.IsTrained && 
                                                                             (isTrainingLibraryItems.Any()&&!isTrainingLibraryItems.Contains(li.LibraryItemId)));
            ungroupedItemSpec.EnableSplitQuery();
            ungroupedItemSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));

            var ungroupedItems = await _libraryItemService.GetAllWithSpecAndWithOutFilterAsync(ungroupedItemSpec);
            if (ungroupedItems.Data is null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002
                    , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            var ungroupedItemsValue = (List<LibraryItemDto>)ungroupedItems.Data!;

            foreach (var libraryItem in ungroupedItemsValue)
            {
                if (processedItemIds.Contains(libraryItem.LibraryItemId))
                {
                    continue;
                }

                processedItemIds.Add(libraryItem.LibraryItemId);
                var mainAuthor = libraryItem.LibraryItemAuthors
                    .First(x => x.LibraryItemId == libraryItem.LibraryItemId)!.Author.FullName;

                // Create list of checked properties compare to root item
                var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
                var listPropertiesChecked = new List<CheckedGroupDetailDto<string>>
                {
                    new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                        {
                            { nameof(libraryItem.CutterNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                            {
                                nameof(libraryItem.ClassificationNumber),
                                (int)FieldGroupCheckedStatus.GroupSuccess
                            },
                            { nameof(Author), (int)FieldGroupCheckedStatus.GroupSuccess },
                            { nameof(libraryItem.Title), (int)FieldGroupCheckedStatus.GroupSuccess },
                            { nameof(libraryItem.SubTitle), (int)FieldGroupCheckedStatus.GroupSuccess }
                        },
                        Item = libraryItem,
                        IsRoot = true
                    }
                };
                var candidateItems = ungroupedItemsValue.Where(
                    li => li.CutterNumber!.Equals(libraryItem.CutterNumber) &&
                          li.ClassificationNumber!.Equals(libraryItem.ClassificationNumber) &&
                          li.LibraryItemAuthors.Any(lia => lia.Author.FullName.Equals(mainAuthor)) &&
                          li.LibraryItemId != libraryItem.LibraryItemId
                ).ToList();

                if (!candidateItems.Any())
                {
                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = (int)FieldGroupCheckedStatus.GroupSuccess,
                        ListCheckedGroupDetail = listPropertiesChecked
                    };
                    response.Add(responseData);
                    continue;
                }
                else
                {
                    candidateItems.RemoveAll(item => processedItemIds.Contains(item.LibraryItemId));

                    foreach (var libraryItemDto in candidateItems)
                    {
                        var itemCheckedResult = new CheckedGroupDetailDto<string>()
                        {
                            PropertiesChecked = new Dictionary<string, int>()
                        };

                        var titleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(libraryItemDto.Title),
                            StringUtils.RemoveSpecialCharacter(libraryItem.Title));

                        int subTitleStatus;
                        if (libraryItem.SubTitle is null && libraryItemDto.SubTitle is null)
                        {
                            subTitleStatus = titleStatus;
                        }
                        else if (libraryItem.SubTitle is null && libraryItemDto.SubTitle != null)
                        {
                            subTitleStatus = CompareFieldStatus(
                                StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
                                StringUtils.RemoveSpecialCharacter(libraryItem.Title));
                        }
                        else if (libraryItem.SubTitle != null && libraryItemDto.SubTitle != null)
                        {
                            subTitleStatus = CompareFieldStatus(
                                StringUtils.RemoveSpecialCharacter(libraryItemDto.SubTitle ?? ""),
                                StringUtils.RemoveSpecialCharacter(libraryItem.SubTitle ?? ""));
                        }
                        else
                        {
                            subTitleStatus = CompareFieldStatus(
                                StringUtils.RemoveSpecialCharacter(libraryItem.SubTitle ?? ""),
                                StringUtils.RemoveSpecialCharacter(libraryItemDto.Title));
                        }

                        var isSubTitleNull = string.IsNullOrEmpty(libraryItem.SubTitle);

                        var titleSubTitleStatus =
                            CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
                        if (titleSubTitleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                        {
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.CutterNumber)
                                , (int)FieldGroupCheckedStatus.GroupSuccess);
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.ClassificationNumber)
                                , (int)FieldGroupCheckedStatus.GroupSuccess);
                            itemCheckedResult.PropertiesChecked.Add(nameof(Author)
                                , (int)FieldGroupCheckedStatus.GroupSuccess);
                            itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                            determineOverallStatus.Add(itemCheckedResult);
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.Title), titleStatus);
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.SubTitle),
                                subTitleStatus);
                            itemCheckedResult.Item = libraryItemDto;
                            itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                            listPropertiesChecked.Add(itemCheckedResult);
                            processedItemIds.Add(libraryItemDto.LibraryItemId);
                        }
                    }

                    var overallStatus = DetermineOverallStatus(determineOverallStatus);

                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = overallStatus,
                        ListCheckedGroupDetail = listPropertiesChecked
                    };
                    response.Add(responseData);
                }
            }

            return new ServiceResult(
                ResultCodeConst.AIService_Success0005,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0005),
                response);
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            throw new Exception("Error invoke when Get and Grade All Suitable Items For Grouping");
        }
    }

    public async Task<IServiceResult> IsAvailableToTrain()
    {
        // Determine current system language
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;

        var baseSession =
            new BaseSpecification<AITrainingSession>(ts => ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
        baseSession.EnableSplitQuery();
        baseSession.ApplyInclude(q => q.Include(s => s.TrainingDetails));
        var session = await _aiTrainingSessionService.GetWithSpecAsync(baseSession);
        if (session.Data is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0009);
            return new ServiceResult(ResultCodeConst.AIService_Warning0009,
                isEng
                    ? errMsg
                    : "Đang có một tiến trình huấn luyện AI diễn ra!");
        }

        return new ServiceResult(ResultCodeConst.AIService_Success0007,
            await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0007));
    }

    public async Task<IServiceResult> NumberOfGroupForTraining()
    {
        return new ServiceResult(ResultCodeConst.SYS_Success0002
            , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            _monitor.AvailableGroupToTrain);
    }


    public async Task<IServiceResult> ExtendModelTraining(IDictionary<int, List<int>> itemIdsDic,
        IDictionary<int, List<string>> imagesDic, string email)
    {
        try
        {
            var listItemIds = itemIdsDic.Values.SelectMany(x => x).ToList();
            var trainingDataDic = new Dictionary<Guid, List<string>>();
            foreach (var key in itemIdsDic.Keys)
            {
                // List Item that in the same group
                var representItemIdForGroup = itemIdsDic[key].First();
                var otherItemIds = itemIdsDic[key].Skip(1).ToList();

                var availableGroup = await GetAvailableGroup(email, representItemIdForGroup, otherItemIds);
                if (availableGroup.Data is null)
                {
                    return availableGroup;
                }

                // Get the existed suitable group or create a new one (response include training code)
                var availableGroupValue = (ItemGroupForAIDto)availableGroup.Data!;

                var code = availableGroupValue.TrainingCode;
                if (!trainingDataDic.ContainsKey(code))
                {
                    trainingDataDic[code] = new List<string>();
                }

                var fileUrls = imagesDic[key];
                trainingDataDic[code].AddRange(fileUrls);
            }

            var backgroundTask = Task.Run(() => ExtendProcessTrainingTask(trainingDataDic, listItemIds, email));

            // Trả về kết quả ngay lập tức
            var successMessage = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003);
            var result = new ServiceResult(ResultCodeConst.AIService_Success0003, successMessage);

            _ = backgroundTask; // Bảo đảm task chạy tiếp trong background

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Grouping Items");
        }
    }

    public async Task<IServiceResult> ExtendModelTraining(TrainedBookDetailDto dto, string email)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            if (dto.TrainingData.Any(x => x.ItemsInGroup.Any(iig
                    => iig.ImageFiles.Count() < 5 || iig.ImageUrls.Count() < 5)))
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0008);
                return new ServiceResult(ResultCodeConst.AIService_Warning0008,
                    isEng
                        ? errMsg
                        : "Vui lòng truyền vào ít nhất đúng 5 ảnh cho mỗi tài liệu để tiến hành train");
            }

            // Merge group from previous stage (input) to db and get code
            var trainingDataDic = new Dictionary<Guid, List<(byte[] FileBytes, string FileName)>>();
            // parameter for creating history
            var itemWithImages = new Dictionary<int, List<string>>();   
            foreach (var untrainedGroup in dto.TrainingData)
            {
                // add value for itemWithImages 
                foreach (var itemWithImagesForTraining in untrainedGroup.ItemsInGroup)
                {
                    itemWithImages.Add(itemWithImagesForTraining.LibraryItemId
                        , itemWithImagesForTraining.ImageUrls);
                }

                var representItemInGroup = untrainedGroup.ItemsInGroup.First();
                var otherItemsInGroup = untrainedGroup.ItemsInGroup
                    .Where(x => x.LibraryItemId != representItemInGroup.LibraryItemId).ToList();
                var otherItemInGroupIds = otherItemsInGroup.Select(x => x.LibraryItemId)
                    .ToList();
                // Get available group or create new one
                var availableGroup =
                    await GetAvailableGroup(email, representItemInGroup.LibraryItemId, otherItemInGroupIds);
                if (availableGroup.Data is null)
                {
                    return availableGroup;
                }

                var availableGroupValue = (ItemGroupForAIDto)availableGroup.Data!;

                var code = availableGroupValue.TrainingCode;
                if (!trainingDataDic.ContainsKey(code))
                {
                    trainingDataDic[code] = new List<(byte[] FileBytes, string FileName)>();
                }

                var files = untrainedGroup.ItemsInGroup.SelectMany(x => x.ImageFiles).ToList();
                foreach (var file in files)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    trainingDataDic[code].Add((memoryStream.ToArray(), file.FileName));
                }
            }

            var backgroundTask = Task.Run(() => ExtendProcessTrainingTask(trainingDataDic, itemWithImages, email));

            // Trả về kết quả ngay lập tức
            var successMessage = await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003);
            var result = new ServiceResult(ResultCodeConst.AIService_Success0003, successMessage);

            _ = backgroundTask; // Bảo đảm task chạy tiếp trong background

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Grouping Items");
        }
    }

    private async Task ExtendProcessTrainingTask(
        Dictionary<Guid, List<(byte[] FileBytes, string FileName)>> trainingDataDic,
        Dictionary<int, List<string>> detailParam, string email)
    {
        try
        {
            // define services that use in background task
            using var scope = _service.CreateScope();
            var libraryItemService = scope.ServiceProvider.GetRequiredService<ILibraryItemService<LibraryItemDto>>();
            var libraryItemGroupService =
                scope.ServiceProvider.GetRequiredService<ILibraryItemGroupService<LibraryItemGroupDto>>();
            var aiTrainingDetailService = scope.ServiceProvider
                .GetRequiredService<IAITrainingDetailService<AITrainingDetailDto>>();
            var aiTrainingSessionService = scope.ServiceProvider
                .GetRequiredService<IAITrainingSessionService<AITrainingSessionDto>>();
            var aiTrainingImageService = scope.ServiceProvider
                .GetRequiredService<IAITraningImageService<AITrainingImageDto>>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AiHub>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
            var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CustomVisionSettings>>();
            var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            // define monitor value
            var currentAiConfiguration = monitor.CurrentValue;

            //Get or create new tag
            // Get or create new tag
            var baseConfig = new BaseConfigurationBackgroudDto
            {
                Client = httpClient,
                Configuration = currentAiConfiguration,
                Logger = logger,
                BaseUrl = string.Format(currentAiConfiguration.BaseAIUrl,
                    currentAiConfiguration.TrainingEndpoint, currentAiConfiguration.ProjectId)
            };
            List<TagDto> tags = await GetTagAsync(baseConfig);
            //Create session
            var initSession = new AITrainingSessionDto()
            {
                Model = AIModel.AzureAIVision,
                TotalTrainedItem = detailParam.Keys.Count,
                TrainingStatus = AITrainingStatus.InProgress,
                TrainDate = DateTime.Now,
                TrainBy = email,
                TrainingDetails = new List<AITrainingDetailDto>()
            };

            // Create relative object
            foreach (var itemId in detailParam.Keys)
            {
                var detail = new AITrainingDetailDto()
                {
                    LibraryItemId = itemId,
                    TrainingImages = new List<AITrainingImageDto>()
                };

                var itemImages = detailParam[itemId];
                var aiTrainingImageDtos = new List<AITrainingImageDto>();
                foreach (var itemImage in itemImages)
                {
                    var aiTrainingImageDto = new AITrainingImageDto()
                    {
                        ImageUrl = itemImage,
                    };
                    aiTrainingImageDtos.Add(aiTrainingImageDto);
                }

                // Add training images to training detail
                detail.TrainingImages = aiTrainingImageDtos;
                // Add training detail to session
                initSession.TrainingDetails.Add(detail);
            }

            // Add all session, detail and image to db
            var createSession = await aiTrainingSessionService.CreateAsync(initSession);
            var sessionEntity = mapper.Map<AITrainingSession>(initSession);
            if (createSession.ResultCode != ResultCodeConst.SYS_Success0001)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully. Err when create Session");
                await Task.CompletedTask;
            }

            // Handle uploading image to tag in ai cloud
            foreach (var (key, value) in trainingDataDic)
            {
                TagDto tag = tags.FirstOrDefault(x => x.Name == key.ToString()) ??
                             await CreateTagAsync(baseConfig, key);
                var memoryStreams = new List<(MemoryStream Stream, string FileName)>();
                // Get cover image of current 

                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(lig
                    => lig.AiTrainingCode.Equals(key.ToString()));
                groupBaseSpec.ApplyInclude(q => q.Include(lig => lig.LibraryItems));

                var group = await libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.Data == null)
                {
                    await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                        new { message = "Error retrieving group", key });
                    await Task.CompletedTask;
                }

                var groupValue = (LibraryItemGroupDto)group.Data!;

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

                foreach (var valueTuple in value)
                {
                    var memoryStream = new MemoryStream(valueTuple.FileBytes);
                    memoryStreams.Add((memoryStream, $"{key}" + valueTuple.FileName));
                }

                await CreateImagesFromDataAsync(baseConfig, memoryStreams, tag.Id);
            }

            await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                new { message = 20, session = initSession.TrainingSessionId });
            // Train the model after adding the images
            var iteration = await TrainProjectAsync(baseConfig);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 30, session = initSession.TrainingSessionId }
            );
            if (iteration is null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            }

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 40, session = initSession.TrainingSessionId }
            );
            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 50, session = initSession.TrainingSessionId }
            );
            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(baseConfig, iteration.Id, monitor.CurrentValue.PublishedName);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 60, session = initSession.TrainingSessionId }
            );
            await libraryItemService.UpdateTrainingStatusAsync(detailParam.Keys.ToList());
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 80, session = initSession.TrainingSessionId }
            );
            initSession.TrainingStatus = AITrainingStatus.Completed;
            await aiTrainingSessionService.UpdateAsync(sessionEntity.TrainingSessionId, initSession);
            await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                new { message = 90, session = initSession.TrainingSessionId });
            //Send notification when finish
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 100, session = initSession.TrainingSessionId }
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Grouping Items");
        }
    }

    private async Task ExtendProcessTrainingTask(Dictionary<Guid, List<string>> trainingDataDic
        , List<int> listItemIds, string email)
    {
        // define services that use in background task
        using var scope = _service.CreateScope();
        var libraryItemService = scope.ServiceProvider.GetRequiredService<ILibraryItemService<LibraryItemDto>>();
        var libraryItemGroupService =
            scope.ServiceProvider.GetRequiredService<ILibraryItemGroupService<LibraryItemGroupDto>>();
        var aiTrainingDetailService = scope.ServiceProvider
            .GetRequiredService<IAITrainingDetailService<AITrainingDetailDto>>();
        var aiTrainingSessionService = scope.ServiceProvider
            .GetRequiredService<IAITrainingSessionService<AITrainingSessionDto>>();
        var aiTrainingImageService = scope.ServiceProvider
            .GetRequiredService<IAITraningImageService<AITrainingImageDto>>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AiHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
        var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CustomVisionSettings>>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        // define monitor value
        var currentAiConfiguration = monitor.CurrentValue;
        try
        {
            // Get or create new tag
            var baseConfig = new BaseConfigurationBackgroudDto
            {
                Client = httpClient,
                Configuration = currentAiConfiguration,
                Logger = logger,
                BaseUrl = string.Format(currentAiConfiguration.BaseAIUrl,
                    currentAiConfiguration.TrainingEndpoint, currentAiConfiguration.ProjectId)
            };
            List<TagDto> tags = await GetTagAsync(baseConfig);
            //Create session
            var initSession = new AITrainingSessionDto()
            {
                Model = AIModel.AzureAIVision,
                TotalTrainedItem = listItemIds.Count,
                TrainingStatus = AITrainingStatus.InProgress,
                TrainDate = DateTime.Now,
                TrainBy = email
            };
            var sessionEntity = mapper.Map<AITrainingSessionDto>(initSession);

            var createSession = await aiTrainingSessionService.CreateAsync(initSession);
            if (createSession.ResultCode != ResultCodeConst.SYS_Success0001)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully. Err when create Session");
                return;
            }

            // Create relative object
            // foreach (var itemId in listItemIds)
            // {
            //     var detail = new AITrainingDetailDto()
            //     {
            //         LibraryItemId = itemId,
            //         TrainingSessionId = sessionEntity.TrainingSessionId
            //     };
            //     var createdTrainingDetail = await aiTrainingDetailService.CreateAsync(detail);
            //     if (createdTrainingDetail.ResultCode != ResultCodeConst.SYS_Success0001)
            //     {
            //         await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully. Err when create training detail");
            //         return;
            //     }
            //      
            //
            // }
            foreach (var (trainingCode, imageUrls) in trainingDataDic)
            {
                TagDto tag = tags.FirstOrDefault(x => x.Name == trainingCode.ToString()) ??
                             await CreateTagAsync(baseConfig, trainingCode);

                var memoryStreams = new List<(MemoryStream Stream, string FileName)>();

                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(lig
                    => lig.AiTrainingCode.Equals(trainingCode.ToString()));
                groupBaseSpec.ApplyInclude(q => q.Include(lig => lig.LibraryItems));

                var group = await libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.Data == null)
                {
                    await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                        new { message = "Error retrieving group", trainingCode });
                    continue;
                }

                var groupValue = (LibraryItemGroupDto)group.Data!;
                var libraryItemIds = groupValue.LibraryItems.Select(lib => lib.LibraryItemId).ToList();

                foreach (var groupValueLibraryItem in groupValue.LibraryItems.Where(li => !li.IsTrained))
                {
                    var response = await httpClient.GetAsync(groupValueLibraryItem.CoverImage);
                    if (!response.IsSuccessStatusCode)
                    {
                        await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                            new
                            {
                                message = $"Failed to get cover image: {groupValueLibraryItem.CoverImage}", trainingCode
                            });
                        return;
                    }

                    var memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                    memoryStreams.Add((memoryStream, $"{groupValueLibraryItem.LibraryItemId}_cover.jpg"));
                }

                // Get images import from user
                foreach (var imageUrl in imageUrls)
                {
                    var response = await httpClient.GetAsync(imageUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                            new { message = $"Failed to get image: {imageUrl}", trainingCode });
                        continue;
                    }

                    var memoryStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                    memoryStreams.Add((memoryStream, $"{trainingCode}_image_{memoryStreams.Count + 1}.jpg"));
                }

                await CreateImagesFromDataAsync(baseConfig, memoryStreams, tag.Id);
            }

            await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                new { message = 20, session = initSession.TrainingSessionId });
            // Train the model after adding the images
            var iteration = await TrainProjectAsync(baseConfig);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 30, session = initSession.TrainingSessionId }
            );
            if (iteration is null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            }

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 40, session = initSession.TrainingSessionId }
            );
            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 50, session = initSession.TrainingSessionId }
            );
            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(baseConfig, iteration.Id, monitor.CurrentValue.PublishedName);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 60, session = initSession.TrainingSessionId }
            );
            await libraryItemService.UpdateTrainingStatusAsync(listItemIds);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 80, session = initSession.TrainingSessionId }
            );
            initSession.TrainingStatus = AITrainingStatus.Completed;
            await aiTrainingSessionService.UpdateAsync(sessionEntity.TrainingSessionId, initSession);
            await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                new { message = 90, session = initSession.TrainingSessionId });
            //Send notification when finish
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new { message = 100, session = initSession.TrainingSessionId }
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


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
                Item = libraryItemValue,
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
                    var titleSubTitleStatus =
                        CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
                    itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                    determineOverallStatus.Add(itemCheckedResult);
                    itemCheckedResult.PropertiesChecked.Add(nameof(libraryItemValue.Title), titleStatus);
                    itemCheckedResult.PropertiesChecked.Add(nameof(libraryItemValue.SubTitle), subTitleStatus);
                    itemCheckedResult.Item = candidateItemValue;
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
        // if (isSubTitleNull)
        // {
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
        // }
        // else
        // {
        //     return (titleStatus, subTitleStatus) switch
        //     {
        //         ((int)FieldGroupCheckedStatus.GroupFailed, (int)FieldGroupCheckedStatus.GroupFailed) => (int)
        //             FieldGroupCheckedStatus.GroupFailed,
        //         ((int)FieldGroupCheckedStatus.GroupFailed, (int)FieldGroupCheckedStatus.GroupSuccess) => (int)
        //             FieldGroupCheckedStatus.GroupSuccess,
        //         ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
        //             (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
        //         ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupFailed)) =>
        //             (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
        //         ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
        //             FieldGroupCheckedStatus.GroupSuccess),
        //         ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.AbleToForceGrouped))
        //             => (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
        //         ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupSuccess)) =>
        //             (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
        //         ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
        //             (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
        //         ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
        //             FieldGroupCheckedStatus.GroupSuccess),
        //         _ => (int)(FieldGroupCheckedStatus.GroupFailed)
        //     };
        // }
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
            // get current LibraryItem 
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == currentLibraryItemId);
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author));
            var libraryItem = await _libraryItemService.GetWithSpecAsync(baseSpec);
            var libraryItemValue = (LibraryItemDto)libraryItem.Data!;
            var mainAuthor =
                libraryItemValue.LibraryItemAuthors.First(x => x.LibraryItemId == currentLibraryItemId)!
                    .Author.FullName;

            var groupSpec = new BaseSpecification<LibraryItemGroup>(lig =>
                lig.CutterNumber == libraryItemValue.CutterNumber &&
                lig.ClassificationNumber == libraryItemValue.ClassificationNumber &&
                lig.Author.Equals(mainAuthor));

            var potentialGroups = await _libraryItemGroupService.GetAllWithSpecAsync(groupSpec);

            if (potentialGroups.Data is null || !((List<LibraryItemGroupDto>)potentialGroups.Data).Any())
                return 0;

            var groupList = (List<LibraryItemGroupDto>)potentialGroups.Data!;
            int bestGroupId = 0;
            double bestScore = 0;

            foreach (var group in groupList)
            {
                // calculate Title and SubTitle
                var titleScore = StringUtils.CombinedFuzzinessScore(libraryItemValue.Title, group.Title);
                var subTitleScore = libraryItemValue.SubTitle != null && group.SubTitle != null
                    ? StringUtils.CombinedFuzzinessScore(libraryItemValue.SubTitle, group.SubTitle)
                    : 0;

                // check status for Title and SubTitle
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

                // combine Title and SubTitle status
                var combinedStatus =
                    CombineTitleSubTitleStatus(titleStatus, subTitleStatus, group.SubTitle == null);

                double groupScore = combinedStatus switch
                {
                    (int)FieldGroupCheckedStatus.GroupSuccess => 100,
                    (int)FieldGroupCheckedStatus.AbleToForceGrouped => 50,
                    _ => 0
                };
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

    public async Task<IServiceResult> RecommendBook(int currentItemId)
    {
        var currentBookBaseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == currentItemId);
        currentBookBaseSpec.ApplyInclude(q => q
            // Include category
            .Include(li => li.Category)
            // Include shelf (if any)
            .Include(li => li.Shelf)
            // Include inventory
            .Include(li => li.LibraryItemInventory)
            // Include authors
            .Include(li => li.LibraryItemAuthors)
            .ThenInclude(lia => lia.Author)
            // Include reviews
            .Include(li => li.LibraryItemReviews)
        );
        var currenBook = await _libraryItemService.GetWithSpecAsync(currentBookBaseSpec);
        if (currenBook.Data is null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "book"));
        }

        var currentBookValue = (LibraryItemDto)currenBook.Data!;

        var recommendBookBaseSpec =
            new BaseSpecification<LibraryItem>(li =>
                li.GroupId != currentBookValue.GroupId || li.LibraryItemGroup == null);
        recommendBookBaseSpec.ApplyInclude(q => q
            // Include category
            .Include(li => li.Category)
            // Include shelf (if any)
            .Include(li => li.Shelf)
            // Include inventory
            .Include(li => li.LibraryItemInventory)
            // Include authors
            .Include(li => li.LibraryItemAuthors)
            .ThenInclude(lia => lia.Author)
            // Include reviews
            .Include(li => li.LibraryItemReviews)
        );

        var allItem = await _libraryItemService.GetAllWithSpecAndWithOutFilterAsync(recommendBookBaseSpec);
        if (allItem.Data is null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "items"));
        }

        var allItemValue = (List<LibraryItemDto>)allItem.Data!;
        var scoredBooks = allItemValue
            .Select(b => new
            {
                Book = b,
                Score = CalculateMatchScore(currentBookValue, b),
                MatchedProperties = GetMatchedProperties(currentBookValue, b)
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();
        var recommendedBooks = scoredBooks
            .Select(b => new RecommendBookDetails
            {
                ItemDetailDto = (b.Book).ToLibraryItemDetailDto(),
                MatchedProperties = b.MatchedProperties
            })
            .ToList();
        return new ServiceResult(ResultCodeConst.AIService_Success0004,
            await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0004),
            recommendedBooks);
    }

    public async Task<IServiceResult> RecommendBook(IFormFile image)
    {
        try
        {
            var objectDetects = await _aiDetectionService.DetectAllAsync(image);
            if (!objectDetects.Any())
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
            }

            var bookBox = objectDetects
                .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase)).ToList();
            if (objectDetects
                    .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                    .ToList().Count > 1 || objectDetects
                    .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                    .ToList().Count < 0)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
            }

            // response property
            Dictionary<int, double> itemTotalPoint = new Dictionary<int, double>();
            Dictionary<int, double> matchObjectPoint = new Dictionary<int, double>();
            Dictionary<int, MinimisedMatchResultDto>
                itemOrcMatchResult = new Dictionary<int, MinimisedMatchResultDto>();
            LibraryItemGroupDto groupValue = new LibraryItemGroupDto();
            if (bookBox.Any())
            {
                var targetBox = bookBox.Select(x => x.Box).FirstOrDefault();
                // crop image
                using var imageStream = image.OpenReadStream();
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var croppedImage = CropImages(imageBytes, bookBox.Select(x => x.Box).ToList()).First();
                // count object in book
                var coverObjectCounts = _aiDetectionService.CountObjectsInImage(objectDetects, targetBox);
                // predict
                var content = new ByteArrayContent(croppedImage);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                var imageResponse = await _httpClient.PostAsync(_basePredictUrl, content);
                imageResponse.EnsureSuccessStatusCode();
                var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
                var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
                var bestPrediction =
                    predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
                    x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
                groupBaseSpec.ApplyInclude(q =>
                    q.Include(x => x.LibraryItems)
                        .ThenInclude(be => be.LibraryItemAuthors)
                        .ThenInclude(ea => ea.Author)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemInstances)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.Category)
                        // Include shelf (if any)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.Shelf)
                        // Include inventory
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemInventory)
                        // Include reviews
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemReviews)
                );
                var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return group;
                }

                groupValue = (LibraryItemGroupDto)group.Data!;
                foreach (var groupValueLibraryItem in groupValue.LibraryItems)
                {
                    //count object of item's cover image
                    var coverImage = _httpClient.GetAsync(groupValueLibraryItem.CoverImage).Result;
                    var coverImageFile = new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                        coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                        groupValueLibraryItem.Title)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/octet-stream"
                    };
                    // count object in cover image
                    var itemObjectsDetected = await _aiDetectionService.DetectAllAsync(image);
                    // check if item's cover image has book object
                    Dictionary<string, int> itemCountObjects;
                    if (itemObjectsDetected.Any(r =>
                            r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
                    {
                        var itemBookBox = itemObjectsDetected.Where(r =>
                                r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                            .Select(r => r.Box)
                            .FirstOrDefault();
                        itemCountObjects =
                            _aiDetectionService.CountObjectsInImage(itemObjectsDetected, itemBookBox);
                    }
                    else
                    {
                        itemCountObjects = _aiDetectionService.CountObjectsInImage(itemObjectsDetected, null);
                    }

                    // check how many object in cover image that match with object in book
                    // var matchCount = (coverObjectCounts.Keys).Intersect(itemCountObjects.Keys).Count();
                    var matchCount = coverObjectCounts.Count(pair =>
                        itemCountObjects.TryGetValue(pair.Key, out int value) && value == pair.Value);
                    var totalObject = coverObjectCounts.Count;
                    var matchRate = (double)matchCount / totalObject * 100;

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
                    mainAuthor.Add(groupValueLibraryItem.LibraryItemAuthors.First(x
                            => x.LibraryItemId == groupValueLibraryItem.LibraryItemId)!
                        .Author.FullName);

                    var ocrCheck = new CheckedItemDto()
                    {
                        Title = groupValueLibraryItem.Title,
                        Authors = mainAuthor,
                        Publisher = groupValueLibraryItem.Publisher ?? " ",
                        Images = new List<IFormFile>()
                        {
                            new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                                coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                                groupValueLibraryItem.Title)
                            {
                                Headers = new HeaderDictionary(),
                                ContentType = "application/octet-stream"
                            }
                        }
                    };
                    var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
                    var compareResultValue = (MatchResultDto)compareResult.Data!;
                    var ocrPoint = compareResultValue.TotalPoint;
                    itemTotalPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate * 0.5 * 100 + ocrPoint * 0.5);
                    itemOrcMatchResult.Add(groupValueLibraryItem.LibraryItemId,
                        compareResultValue.ToMinimisedMatchResultDto());
                    matchObjectPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate);
                }
            }
            else
            {
                using var imageStream = image.OpenReadStream();
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                var imageResponse = await _httpClient.PostAsync(_basePredictUrl, content);
                imageResponse.EnsureSuccessStatusCode();
                var coverObjectCounts = _aiDetectionService.CountObjectsInImage(objectDetects, null);
                var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
                var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
                var bestPrediction =
                    predictionResult.Predictions.MaxBy(p => p.Probability);
                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
                    x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
                groupBaseSpec.EnableSplitQuery();
                groupBaseSpec.ApplyInclude(q =>
                    q.Include(x => x.LibraryItems)
                        .ThenInclude(be => be.LibraryItemAuthors)
                        .ThenInclude(ea => ea.Author)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemInstances)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.Category)
                        // Include shelf (if any)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.Shelf)
                        // Include inventory
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemInventory)
                        // Include reviews
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemReviews)
                );
                var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return group;
                }

                groupValue = (LibraryItemGroupDto)group.Data!;
                foreach (var groupValueLibraryItem in groupValue.LibraryItems)
                {
                    //count object of item's cover image
                    var coverImage = _httpClient.GetAsync(groupValueLibraryItem.CoverImage).Result;
                    var coverImageFile = new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                        coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                        groupValueLibraryItem.Title)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/octet-stream"
                    };
                    // count object in cover image
                    var itemObjectsDetected = await _aiDetectionService.DetectAllAsync(image);
                    // check if item's cover image has book object
                    Dictionary<string, int> itemCountObjects;
                    if (itemObjectsDetected.Any(r =>
                            r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
                    {
                        var itemBookBox = itemObjectsDetected.Where(r =>
                                r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                            .Select(r => r.Box)
                            .FirstOrDefault();
                        itemCountObjects =
                            _aiDetectionService.CountObjectsInImage(itemObjectsDetected, itemBookBox);
                    }
                    else
                    {
                        itemCountObjects = _aiDetectionService.CountObjectsInImage(itemObjectsDetected, null);
                    }

                    // check how many object in cover image that match with object in book
                    // var matchCount = (coverObjectCounts.Keys).Intersect(itemCountObjects.Keys).Count();
                    var matchCount = coverObjectCounts.Count(pair =>
                        itemCountObjects.TryGetValue(pair.Key, out int value) && value == pair.Value);
                    var totalObject = coverObjectCounts.Count;
                    var matchRate = (double)matchCount / totalObject;

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
                    mainAuthor.Add(groupValueLibraryItem.LibraryItemAuthors.First(x
                            => x.LibraryItemId == groupValueLibraryItem.LibraryItemId)!
                        .Author.FullName);

                    var ocrCheck = new CheckedItemDto()
                    {
                        Title = groupValueLibraryItem.Title,
                        Authors = mainAuthor,
                        Publisher = groupValueLibraryItem.Publisher ?? " ",
                        Images = new List<IFormFile>()
                        {
                            image
                        }
                    };
                    var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
                    var compareResultValue = (List<MatchResultDto>)compareResult.Data!;
                    var ocrPoint = compareResultValue.First().TotalPoint;
                    itemTotalPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate * 0.5 * 100 + ocrPoint * 0.5);
                    itemOrcMatchResult.Add(groupValueLibraryItem.LibraryItemId,
                        compareResultValue.First().ToMinimisedMatchResultDto());
                    matchObjectPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate);
                }
            }

            int bestItemId = itemTotalPoint.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            var currentBookValue = groupValue.LibraryItems.Where(li => li.LibraryItemId == bestItemId).First();
            var recommendBookBaseSpec =
                new BaseSpecification<LibraryItem>(li =>
                    li.GroupId != currentBookValue.GroupId || li.LibraryItemGroup == null);
            recommendBookBaseSpec.ApplyInclude(q =>
                q.Include(x => x.LibraryItemAuthors)
                    .ThenInclude(ea => ea.Author));

            var allItem = await _libraryItemService.GetAllWithSpecAndWithOutFilterAsync(recommendBookBaseSpec);
            if (allItem.Data is null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                        "items"));
            }

            var allItemValue = (List<LibraryItemDto>)allItem.Data!;
            var scoredBooks = allItemValue
                .Select(b => new
                {
                    Book = b,
                    Score = CalculateMatchScore(currentBookValue, b),
                    MatchedProperties = GetMatchedProperties(currentBookValue, b)
                })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();
            var recommendedBooks = scoredBooks
                .Select(b => new RecommendBookDetails
                {
                    ItemDetailDto = (b.Book).ToLibraryItemDetailDto(),
                    MatchedProperties = b.MatchedProperties
                })
                .ToList();
            return new ServiceResult(ResultCodeConst.AIService_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0004),
                recommendedBooks);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Recommend Book");
        }
    }

    private int CalculateMatchScore(LibraryItemDto currentBook, LibraryItemDto otherBook)
    {
        int score = 0;
        //get main Author of otherBook
        string currentBookMainAuthor = currentBook.LibraryItemAuthors.First(x
                => x.LibraryItemId == currentBook.LibraryItemId)!
            .Author.FullName;
        string otherBookMainAuthor = otherBook.LibraryItemAuthors.First(x
            => x.LibraryItemId == otherBook.LibraryItemId)!.Author.FullName;
        //get gernes
        List<string> currentBookGenres = currentBook.Genres!.Split(",").ToList();
        List<string> otherBookGenres = otherBook.Genres!.Split(",").ToList();
        if (currentBook.Title == otherBook.Title) score++;
        if (currentBook.SubTitle == otherBook.SubTitle) score++;
        if (currentBook.Language == otherBook.Language) score++;
        if (currentBook.OriginLanguage == otherBook.OriginLanguage) score++;
        if (currentBook.PublicationYear == otherBook.PublicationYear) score++;
        if (currentBook.Publisher == otherBook.Publisher) score++;
        if (currentBook.ClassificationNumber == otherBook.ClassificationNumber) score++;
        if (currentBook.CutterNumber == otherBook.CutterNumber) score++;
        if (currentBookMainAuthor.Equals(otherBookMainAuthor)) score++;
        if (currentBookGenres.Intersect(otherBookGenres).Any())
            score += currentBookGenres.Intersect(otherBookGenres).Count();
        if (currentBook.Isbn!.Equals(otherBook.Isbn)) score++;
        if (currentBook.PublicationYear == otherBook.PublicationYear) score++;
        return score;
    }

    private List<MatchedProperties> GetMatchedProperties(LibraryItemDto currentBook, LibraryItemDto otherBook)
    {
        return new List<MatchedProperties>
        {
            new MatchedProperties { Name = "Title", IsMatched = currentBook.Title == otherBook.Title },
            new MatchedProperties { Name = "SubTitle", IsMatched = currentBook.SubTitle == otherBook.SubTitle },
            new MatchedProperties { Name = "Language", IsMatched = currentBook.Language == otherBook.Language },
            new MatchedProperties
                { Name = "OriginLanguage", IsMatched = currentBook.OriginLanguage == otherBook.OriginLanguage },
            new MatchedProperties
                { Name = "PublicationYear", IsMatched = currentBook.PublicationYear == otherBook.PublicationYear },
            new MatchedProperties { Name = "Publisher", IsMatched = currentBook.Publisher == otherBook.Publisher },
            new MatchedProperties
            {
                Name = "ClassificationNumber",
                IsMatched = currentBook.ClassificationNumber == otherBook.ClassificationNumber
            },
            new MatchedProperties
                { Name = "CutterNumber", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber }
        };
    }

    // first version of predict function
    // public async Task<IServiceResult> PredictAsync(IFormFile image)
    // {
    //     try
    //     {
    //         // Detect bounding boxes for books
    //         var bookBoxes = await _aiDetectionService.DetectAllAsync(image);
    //         // Count Object In Box
    //         Dictionary<string, int> coverObjectCounts = new Dictionary<string, int>();
    //         if (bookBoxes.Any(r =>
    //                 r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
    //         {
    //             var bookBox = bookBoxes.Where(r =>
    //                     r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
    //                 .Select(r => r.Box)
    //                 .FirstOrDefault();
    //             coverObjectCounts = _aiDetectionService.CountObjectsInImage(bookBoxes, bookBox);
    //         }
    //         coverObjectCounts = _aiDetectionService.CountObjectsInImage(bookBoxes, null);
    //         
    //         // Crop images based on bounding boxes
    //         using (var imageStream = image.OpenReadStream())
    //         using (var ms = new MemoryStream())
    //         {
    //             await imageStream.CopyToAsync(ms);
    //             var imageBytes = ms.ToArray();
    //             if (bookBoxes.Any())
    //             {
    //                 var bookBoxesCoordinates = bookBoxes.Select(x => x.Box).ToList();
    //                 var croppedImages = CropImages(imageBytes, bookBoxesCoordinates);
    //                 var predictResponse = new PredictionResponseDto()
    //                 {
    //                     NumberOfBookDetected = croppedImages.Count,
    //                     LibraryItemPrediction = new List<PossibleLibraryItem>()
    //                 };
    //                 // detect and predict base on cropped images
    //                 foreach (var croppedImage in croppedImages)
    //                 {
    //                     var content = new ByteArrayContent(croppedImage);
    //                     content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    //                     _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
    //                     var response = await _httpClient.PostAsync(_basePredictUrl, content);
    //                     response.EnsureSuccessStatusCode();
    //                     var jsonResponse = await response.Content.ReadAsStringAsync();
    //                     var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
    //                         new JsonSerializerOptions
    //                         {
    //                             PropertyNameCaseInsensitive = true,
    //                         });
    //                     var bestPrediction =
    //                         predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
    //                     var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
    //                         x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
    //                     groupBaseSpec.ApplyInclude(q =>
    //                         q.Include(x => x.LibraryItems)
    //                             .ThenInclude(be => be.LibraryItemAuthors)
    //                             .ThenInclude(ea => ea.Author));
    //                     var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
    //                     if (group.ResultCode != ResultCodeConst.SYS_Success0002)
    //                     {
    //                         return group;
    //                     }
    //                     var groupValue = (LibraryItemGroupDto)group.Data!;
    //                     foreach (var groupValueLibraryItem in groupValue.LibraryItems)
    //                     {
    //                         var coverImage = _httpClient.GetAsync(groupValueLibraryItem.CoverImage).Result;
    //                         var mainAuthor =
    //                             groupValueLibraryItem.LibraryItemAuthors.First(x
    //                                     => x.LibraryItemId == groupValueLibraryItem.LibraryItemId)!
    //                                 .Author.FullName;
    //                         var ocrCheck = new CheckedItemDto()
    //                         {
    //                             Title = groupValueLibraryItem.Title,
    //                             Authors = new List<string>() { mainAuthor },
    //                             Publisher = groupValueLibraryItem.Publisher ?? " ",
    //                             Images = new List<IFormFile>()
    //                             {
    //                                 new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
    //                                     coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
    //                                     groupValueLibraryItem.Title)
    //                                 {
    //                                     Headers = new HeaderDictionary(),
    //                                     ContentType = "application/octet-stream"
    //                                 }
    //                             }
    //                         };
    //                         var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
    //                         var compareResultValue = (MatchResultDto)compareResult.Data!;
    //                         
    //                     }
    //                     
    //                 }
    //
    //                 return new ServiceResult(ResultCodeConst.AIService_Success0003,
    //                     await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0003), predictResponse
    //                 );
    //             }
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
    public async Task<IServiceResult> PredictAsync(IFormFile image)
    {
        try
        {
            var objectDetects = await _aiDetectionService.DetectAllAsync(image);
            if (!objectDetects.Any())
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
            }

            var bookBox = objectDetects
                .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase)).ToList();
            if (objectDetects
                    .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                    .ToList().Count > 1 || objectDetects
                    .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                    .ToList().Count < 0)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
            }

            // response property
            Dictionary<int, double> itemTotalPoint = new Dictionary<int, double>();
            Dictionary<int, double> matchObjectPoint = new Dictionary<int, double>();
            Dictionary<int, MinimisedMatchResultDto>
                itemOrcMatchResult = new Dictionary<int, MinimisedMatchResultDto>();
            LibraryItemGroupDto groupValue = new LibraryItemGroupDto();
            if (bookBox.Any())
            {
                var targetBox = bookBox.Select(x => x.Box).FirstOrDefault();
                // crop image
                using var imageStream = image.OpenReadStream();
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var croppedImage = CropImages(imageBytes, bookBox.Select(x => x.Box).ToList()).First();
                // count object in book
                var coverObjectCounts = _aiDetectionService.CountObjectsInImage(objectDetects, targetBox);
                // predict
                var content = new ByteArrayContent(croppedImage);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                var imageResponse = await _httpClient.PostAsync(_basePredictUrl, content);
                imageResponse.EnsureSuccessStatusCode();
                var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
                var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
                var bestPrediction =
                    predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
                    x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
                groupBaseSpec.ApplyInclude(q =>
                    q.Include(x => x.LibraryItems)
                        .ThenInclude(be => be.LibraryItemAuthors)
                        .ThenInclude(ea => ea.Author));
                var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return group;
                }

                groupValue = (LibraryItemGroupDto)group.Data!;
                foreach (var groupValueLibraryItem in groupValue.LibraryItems)
                {
                    //count object of item's cover image
                    var coverImage = _httpClient.GetAsync(groupValueLibraryItem.CoverImage).Result;
                    var coverImageFile = new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                        coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                        groupValueLibraryItem.Title)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/octet-stream"
                    };
                    // count object in cover image
                    var itemObjectsDetected = await _aiDetectionService.DetectAllAsync(image);
                    // check if item's cover image has book object
                    Dictionary<string, int> itemCountObjects;
                    if (itemObjectsDetected.Any(r =>
                            r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
                    {
                        var itemBookBox = itemObjectsDetected.Where(r =>
                                r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                            .Select(r => r.Box)
                            .FirstOrDefault();
                        itemCountObjects =
                            _aiDetectionService.CountObjectsInImage(itemObjectsDetected, itemBookBox);
                    }
                    else
                    {
                        itemCountObjects = _aiDetectionService.CountObjectsInImage(itemObjectsDetected, null);
                    }

                    // check how many object in cover image that match with object in book
                    // var matchCount = (coverObjectCounts.Keys).Intersect(itemCountObjects.Keys).Count();
                    var matchCount = coverObjectCounts.Count(pair =>
                        itemCountObjects.TryGetValue(pair.Key, out int value) && value == pair.Value);
                    var totalObject = coverObjectCounts.Count;
                    var matchRate = (double)matchCount / totalObject;

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
                    mainAuthor.Add(groupValueLibraryItem.LibraryItemAuthors.First(x
                            => x.LibraryItemId == groupValueLibraryItem.LibraryItemId)!
                        .Author.FullName);

                    var ocrCheck = new CheckedItemDto()
                    {
                        Title = groupValueLibraryItem.Title,
                        Authors = mainAuthor,
                        Publisher = groupValueLibraryItem.Publisher ?? " ",
                        Images = new List<IFormFile>()
                        {
                            new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                                coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                                groupValueLibraryItem.Title)
                            {
                                Headers = new HeaderDictionary(),
                                ContentType = "application/octet-stream"
                            }
                        }
                    };
                    var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
                    var compareResultValue = (MatchResultDto)compareResult.Data!;
                    var ocrPoint = compareResultValue.TotalPoint;
                    itemTotalPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate * 0.5 * 100 + ocrPoint * 0.5);
                    itemOrcMatchResult.Add(groupValueLibraryItem.LibraryItemId,
                        compareResultValue.ToMinimisedMatchResultDto());
                    matchObjectPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate);
                }
            }
            else
            {
                using var imageStream = image.OpenReadStream();
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();
                var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
                var imageResponse = await _httpClient.PostAsync(_basePredictUrl, content);
                imageResponse.EnsureSuccessStatusCode();
                var coverObjectCounts = _aiDetectionService.CountObjectsInImage(objectDetects, null);
                var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
                var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
                var bestPrediction =
                    predictionResult.Predictions.MaxBy(p => p.Probability);
                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
                    x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
                groupBaseSpec.EnableSplitQuery();
                groupBaseSpec.ApplyInclude(q =>
                    q.Include(x => x.LibraryItems)
                        .ThenInclude(be => be.LibraryItemAuthors)
                        .ThenInclude(ea => ea.Author)
                        .Include(x => x.LibraryItems)
                        .ThenInclude(li => li.LibraryItemInstances));
                var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
                if (group.ResultCode != ResultCodeConst.SYS_Success0002)
                {
                    return group;
                }

                groupValue = (LibraryItemGroupDto)group.Data!;
                foreach (var groupValueLibraryItem in groupValue.LibraryItems)
                {
                    //count object of item's cover image
                    var coverImage = _httpClient.GetAsync(groupValueLibraryItem.CoverImage).Result;
                    var coverImageFile = new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                        coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                        groupValueLibraryItem.Title)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/octet-stream"
                    };
                    // count object in cover image
                    var itemObjectsDetected = await _aiDetectionService.DetectAllAsync(image);
                    // check if item's cover image has book object
                    Dictionary<string, int> itemCountObjects;
                    if (itemObjectsDetected.Any(r =>
                            r.Name.Equals("book", StringComparison.OrdinalIgnoreCase)))
                    {
                        var itemBookBox = itemObjectsDetected.Where(r =>
                                r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
                            .Select(r => r.Box)
                            .FirstOrDefault();
                        itemCountObjects =
                            _aiDetectionService.CountObjectsInImage(itemObjectsDetected, itemBookBox);
                    }
                    else
                    {
                        itemCountObjects = _aiDetectionService.CountObjectsInImage(itemObjectsDetected, null);
                    }

                    // check how many object in cover image that match with object in book
                    // var matchCount = (coverObjectCounts.Keys).Intersect(itemCountObjects.Keys).Count();
                    var matchCount = coverObjectCounts.Count(pair =>
                        itemCountObjects.TryGetValue(pair.Key, out int value) && value == pair.Value);
                    var totalObject = coverObjectCounts.Count;
                    var matchRate = (double)matchCount / totalObject * 100;

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
                    mainAuthor.Add(groupValueLibraryItem.LibraryItemAuthors.First(x
                            => x.LibraryItemId == groupValueLibraryItem.LibraryItemId)!
                        .Author.FullName);

                    var ocrCheck = new CheckedItemDto()
                    {
                        Title = groupValueLibraryItem.Title,
                        Authors = mainAuthor,
                        Publisher = groupValueLibraryItem.Publisher ?? " ",
                        Images = new List<IFormFile>()
                        {
                            image
                        },
                        SubTitle = groupValueLibraryItem.SubTitle,
                        GeneralNote = groupValueLibraryItem.GeneralNote
                    };
                    var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
                    var compareResultValue = (List<MatchResultDto>)compareResult.Data!;
                    var ocrPoint = compareResultValue.First().TotalPoint;
                    itemTotalPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate * 0.5 * 100 + ocrPoint * 0.5);
                    itemOrcMatchResult.Add(groupValueLibraryItem.LibraryItemId,
                        compareResultValue.First().ToMinimisedMatchResultDto());
                    matchObjectPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate);
                }
            }

            var response = new PredictionResponseDto()
            {
                OtherItems = new List<ItemPredictedDetailDto>()
            };

            //choose the best item
            int bestItemId = itemTotalPoint.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            var bestItemPredictResponse = new ItemPredictedDetailDto
            {
                OCRResult = itemOrcMatchResult[bestItemId],
                LibraryItemId = bestItemId,
                ObjectMatchResult = (int)Math.Floor(matchObjectPoint[bestItemId])
            };
            itemTotalPoint.Remove(bestItemId);
            response.BestItem = bestItemPredictResponse;
            // add other items detail
            foreach (var groupValueLibraryItemId in itemTotalPoint.Keys)
            {
                var itemPredictResponse = new ItemPredictedDetailDto
                {
                    OCRResult = itemOrcMatchResult[groupValueLibraryItemId],
                    LibraryItemId = groupValueLibraryItemId,
                    ObjectMatchResult = (int)Math.Floor(matchObjectPoint[groupValueLibraryItemId])
                };
                response.OtherItems.Add(itemPredictResponse);
            }

            return new ServiceResult(ResultCodeConst.AIService_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0004), response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Predict Book Model");
        }
    }

    public async Task<IServiceResult> PredictWithEmgu(IFormFile image)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Init store result param
            Dictionary<int, double> itemTotalPoint = new Dictionary<int, double>();
            Dictionary<int, MinimisedMatchResultDto>
                itemOrcMatchResult = new Dictionary<int, MinimisedMatchResultDto>();
            LibraryItemGroupDto groupValue = new LibraryItemGroupDto();
            // predict
            using var imageStream = image.OpenReadStream();
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
            var imageResponse = await _httpClient.PostAsync(_basePredictUrl, content);
            imageResponse.EnsureSuccessStatusCode();
            var jsonResponse = await imageResponse.Content.ReadAsStringAsync();
            var predictionResult = JsonSerializer.Deserialize<PredictResultDto>(jsonResponse,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            var bestPrediction =
                predictionResult.Predictions.OrderByDescending(p => p.Probability).FirstOrDefault();
            var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(x =>
                x.AiTrainingCode.ToString().ToLower().Equals(bestPrediction.TagName));
            groupBaseSpec.ApplyInclude(q =>
                q.Include(x => x.LibraryItems)
                    .ThenInclude(be => be.LibraryItemAuthors)
                    .ThenInclude(ea => ea.Author));
            var group = await _libraryItemGroupService.GetWithSpecAsync(groupBaseSpec);
            if (group.ResultCode != ResultCodeConst.SYS_Success0002)
            {
                return group;
            }

            groupValue = (LibraryItemGroupDto)group.Data!;
            // Calculate the similarity
            var itemsEqualCompare = await _aiDetectionService.DetectWithEmgu(image, groupValue.AiTrainingCode);
            Dictionary<int, double> matchObjectPoint = (Dictionary<int, double>)itemsEqualCompare.Data!;
            if (!matchObjectPoint.Any())
            {
                var errMsg = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                    "item in group");

                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng ? errMsg : "Không tìm thấy tài liệu nào trong nhóm để so sánh");
            }

            // check ocr
            foreach (var item in groupValue.LibraryItems)
            {
                List<string> mainAuthor = new List<string>();
                mainAuthor.Add(item.GeneralNote!);
                mainAuthor.Add(item.LibraryItemAuthors.First(x
                        => x.LibraryItemId == item.LibraryItemId)!
                    .Author.FullName);

                var coverImage = _httpClient.GetAsync(item.CoverImage).Result;
                var ocrCheck = new CheckedItemDto()
                {
                    Title = item.Title,
                    Authors = mainAuthor,
                    Publisher = item.Publisher ?? " ",
                    Images = new List<IFormFile>()
                    {
                        new FormFile(await coverImage.Content.ReadAsStreamAsync(), 0,
                            coverImage.Content.ReadAsByteArrayAsync().Result.Length, "file",
                            item.Title)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = "application/octet-stream"
                        }
                    }
                };
                var compareResult = await _ocrService.CheckBookInformationAsync(ocrCheck);
                var compareResultValue = ((List<MatchResultDto>)compareResult.Data!).First();
                itemOrcMatchResult.Add(item.LibraryItemId,
                    compareResultValue.ToMinimisedMatchResultDto());
                var ocrPoint = compareResultValue.TotalPoint;
                itemTotalPoint.Add(item.LibraryItemId,matchObjectPoint[item.LibraryItemId]*0.5+ocrPoint*0.5);
            }
            
            var response = new PredictionResponseDto()
            {
                OtherItems = new List<ItemPredictedDetailDto>()
            };

            //choose the best item
            int bestItemId = itemTotalPoint.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            var bestItemPredictResponse = new ItemPredictedDetailDto
            {
                OCRResult = itemOrcMatchResult[bestItemId],
                LibraryItemId = bestItemId,
                ObjectMatchResult = (int)Math.Floor(matchObjectPoint[bestItemId])
            };
            itemTotalPoint.Remove(bestItemId);
            response.BestItem = bestItemPredictResponse;
            // add other items detail
            foreach (var groupValueLibraryItemId in itemTotalPoint.Keys)
            {
                var itemPredictResponse = new ItemPredictedDetailDto
                {
                    OCRResult = itemOrcMatchResult[groupValueLibraryItemId],
                    LibraryItemId = groupValueLibraryItemId,
                    ObjectMatchResult = (int)Math.Floor(matchObjectPoint[groupValueLibraryItemId])
                };
                response.OtherItems.Add(itemPredictResponse);
            }

            return new ServiceResult(ResultCodeConst.AIService_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0004), response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when Predict Book Model with Emgu");
        }
    }

    // #region Predict Function
    //   public async Task<IServiceResult> PredictAsync(IFormFile image)
    // {
    //     try
    //     {
    //         var objectDetects = await _aiDetectionService.DetectAllAsync(image);
    //         if (!objectDetects.Any())
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0003,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0003));
    //         }
    //
    //         var bookBoxes = objectDetects
    //             .Where(od => od.Name.Equals("book", StringComparison.OrdinalIgnoreCase))
    //             .ToList();
    //
    //         if (bookBoxes.Count != 1)
    //         {
    //             return new ServiceResult(ResultCodeConst.AIService_Warning0004,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
    //         }
    //
    //         // Common processing
    //         var (croppedImage, coverObjectCounts) = await ProcessImageAndObjects(image, objectDetects, bookBoxes);
    //         var predictionResult = await GetPredictionResult(croppedImage);
    //         var groupValue = await GetLibraryItemGroup(predictionResult);
    //
    //         if (groupValue.ResultCode != ResultCodeConst.SYS_Success0002)
    //         {
    //             return groupValue;
    //         }
    //
    //         var (itemTotalPoint, itemOrcMatchResult) = await ProcessLibraryItems(
    //             groupValue.Data as LibraryItemGroupDto,
    //             coverObjectCounts,
    //             objectDetects
    //         );
    //
    //         var response = BuildResponse(itemTotalPoint, itemOrcMatchResult, groupValue.Data as LibraryItemGroupDto);
    //
    //         return new ServiceResult(ResultCodeConst.AIService_Success0004,
    //             await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0004), response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when Predict Book Model");
    //     }
    // }
    //
    // private async Task<(byte[] Image, Dictionary<string, int> ObjectCounts)> ProcessImageAndObjects(
    //     IFormFile image,
    //     IEnumerable<DetectResultDto> objectDetects,
    //     List<DetectResultDto> bookBoxes)
    // {
    //     var targetBox = bookBoxes.Select(x => x.Box).FirstOrDefault();
    //     var imageBytes = await ReadImageBytes(image);
    //
    //     var croppedImage = bookBoxes.Any()
    //         ? CropImages(imageBytes, bookBoxes.Select(x => x.Box).ToList()).First()
    //         : imageBytes;
    //
    //     var objectCounts = _aiDetectionService.CountObjectsInImage(objectDetects.ToList(), targetBox);
    //     return (croppedImage, objectCounts);
    // }
    //
    // private async Task<PredictResultDto> GetPredictionResult(byte[] imageData)
    // {
    //     using var content = new ByteArrayContent(imageData);
    //     content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    //     _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _monitor.PredictionKey);
    //
    //     var response = await _httpClient.PostAsync(_basePredictUrl, content);
    //     response.EnsureSuccessStatusCode();
    //
    //     return JsonSerializer.Deserialize<PredictResultDto>(
    //         await response.Content.ReadAsStringAsync(),
    //         new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    // }
    //
    // private async Task<IServiceResult> GetLibraryItemGroup(PredictResultDto predictionResult)
    // {
    //     var bestPrediction = predictionResult.Predictions.MaxBy(p => p.Probability);
    //     var groupSpec = new BaseSpecification<LibraryItemGroup>(x =>
    //         x.AiTrainingCode.ToString().Equals(bestPrediction.TagName, StringComparison.OrdinalIgnoreCase));
    //
    //     groupSpec.ApplyInclude(q =>
    //         q.Include(x => x.LibraryItems)
    //             .ThenInclude(be => be.LibraryItemAuthors)
    //             .ThenInclude(ea => ea.Author)
    //             .Include(x=>x.LibraryItems)
    //             .ThenInclude(li => li.Category)
    //             .Include(x=>x.LibraryItems)
    //             .ThenInclude(li => li.LibraryItemReviews));
    //
    //     return await _libraryItemGroupService.GetWithSpecAsync(groupSpec);
    // }
    //
    // private async Task<(Dictionary<int, double>, Dictionary<int, MatchResultDto>)> ProcessLibraryItems(
    //     LibraryItemGroupDto group,
    //     Dictionary<string, int> coverObjectCounts,
    //     IEnumerable<DetectResultDto> objectDetects)
    // {
    //     var itemTotalPoint = new Dictionary<int, double>();
    //     var itemOrcMatchResult = new Dictionary<int, MatchResultDto>();
    //
    //     foreach (var item in group.LibraryItems)
    //     {
    //         var (matchRate, ocrPoint) = await ProcessSingleItem(item, coverObjectCounts, objectDetects);
    //         itemTotalPoint.Add(item.LibraryItemId, matchRate * 0.5 + ocrPoint * 0.5);
    //     }
    //
    //     return (itemTotalPoint, itemOrcMatchResult);
    // }
    //
    // private async Task<(double MatchRate, double OcrPoint)> ProcessSingleItem(
    //     LibraryItemDto item,
    //     Dictionary<string, int> coverObjectCounts,
    //     IEnumerable<DetectResultDto> objectDetects)
    // {
    //     var coverImage = await _httpClient.GetAsync(item.CoverImage);
    //     var coverImageFile = await CreateFormFile(coverImage, item.Title);
    //
    //     var itemObjects = await _aiDetectionService.DetectAllAsync(coverImageFile);
    //     var itemCounts = CountObjectsWithBookCheck(itemObjects);
    //
    //     var matchCount = coverObjectCounts.Count(pair =>
    //         itemCounts.TryGetValue(pair.Key, out int value) && value == pair.Value);
    //
    //     var matchRate = (double)matchCount / coverObjectCounts.Count;
    //     var ocrPoint = await GetOcrPoint(item, coverImage);
    //
    //     return (matchRate, ocrPoint);
    // }
    //
    // private Dictionary<string, int> CountObjectsWithBookCheck(IEnumerable<DetectResultDto> detects)
    // {
    //     var bookBox = detects
    //         .FirstOrDefault(r => r.Name.Equals("book", StringComparison.OrdinalIgnoreCase))?
    //         .Box;
    //
    //     return _aiDetectionService.CountObjectsInImage(detects.ToList(), bookBox);
    // }
    //
    // private async Task<byte[]> ReadImageBytes(IFormFile image)
    // {
    //     await using var stream = image.OpenReadStream();
    //     using var ms = new MemoryStream();
    //     await stream.CopyToAsync(ms);
    //     return ms.ToArray();
    // }
    //
    // private async Task<IFormFile> CreateFormFile(HttpResponseMessage response, string title)
    // {
    //     return new FormFile(
    //         await response.Content.ReadAsStreamAsync(),
    //         0,
    //         (await response.Content.ReadAsByteArrayAsync()).Length,
    //         "file",
    //         title)
    //     {
    //         Headers = new HeaderDictionary(),
    //         ContentType = "application/octet-stream"
    //     };
    // }
    //
    // private async Task<double> GetOcrPoint(LibraryItemDto item, HttpResponseMessage coverImage)
    // {
    //     var ocrCheck = new CheckedItemDto
    //     {
    //         Title = item.Title,
    //         Authors = new List<string> { GetMainAuthor(item) },
    //         Publisher = item.Publisher ?? " ",
    //         Images = new List<IFormFile> { await CreateFormFile(coverImage, item.Title) }
    //     };
    //
    //     var result = await _ocrService.CheckBookInformationAsync(ocrCheck);
    //     return ((MatchResultDto)result.Data!).TotalPoint;
    // }
    //
    // private string GetMainAuthor(LibraryItemDto item)
    // {
    //     return item.GeneralNote ?? item.LibraryItemAuthors
    //         .First(x => x.LibraryItemId == item.LibraryItemId)
    //         .Author.FullName;
    // }
    //
    // private PredictionResponseDto BuildResponse(
    //     Dictionary<int, double> itemPoints,
    //     Dictionary<int, MatchResultDto> matchResults,
    //     LibraryItemGroupDto group)
    // {
    //     var bestItemId = itemPoints.MaxBy(x => x.Value).Key;
    //     var bestItem = group.LibraryItems.First(x => x.LibraryItemId == bestItemId);
    //
    //     return new PredictionResponseDto
    //     {
    //         BestItem = CreateItemDetail(bestItem, matchResults[bestItemId]),
    //         OtherItems = group.LibraryItems
    //             .Where(x => x.LibraryItemId != bestItemId)
    //             .Select(x => CreateItemDetail(x, matchResults[x.LibraryItemId]))
    //             .ToList()
    //     };
    // }
    //
    // private ItemPredictedDetailDto CreateItemDetail(LibraryItemDto item, MatchResultDto matchResult)
    // {
    //     return new ItemPredictedDetailDto
    //     {
    //         OCRResult = matchResult,
    //         LibraryItemDetail = item.ToLibraryItemDetailDto()
    //     };
    // }
    // #endregion
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