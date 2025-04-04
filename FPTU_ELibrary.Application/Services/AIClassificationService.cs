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
using FPTU_ELibrary.Application.Exceptions;
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
    private readonly string _basePredictUrl;
    
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _service;
    
    private readonly IOCRService _ocrService;
    private readonly CustomVisionSettings _monitor;
    private readonly ISystemMessageService _msgService;
    private readonly IAIDetectionService _aiDetectionService;
    
    private readonly ICategoryService<CategoryDto> _cateService;
    private readonly IAITrainingSessionService<AITrainingSessionDto> _aiTrainingSessionService;
    private readonly IHubContext<AiHub> _hubContext;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGroupService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;

    public AIClassificationService(
        ILogger logger,
        HttpClient httpClient, 
        IOCRService ocrService,
        IServiceProvider service, 
        ISystemMessageService msgService,
        IAIDetectionService aiDetectionService,
        ICategoryService<CategoryDto> cateService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGroupService,
        IAITrainingSessionService<AITrainingSessionDto> aiTrainingSessionService,
        IHubContext<AiHub> hubContext,
        IOptionsMonitor<CustomVisionSettings> monitor)
    {
        _logger = logger;
        _service = service;
        _ocrService = ocrService;
        _msgService = msgService;
        _httpClient = httpClient;
        _cateService = cateService;
        _aiDetectionService = aiDetectionService;
        _libraryItemService = libraryItemService;
        _aiTrainingSessionService = aiTrainingSessionService;
        _hubContext = hubContext;
        _libraryItemGroupService = libraryItemGroupService;
        
        _monitor = monitor.CurrentValue;
        
        _basePredictUrl = string.Format(
            _monitor.BasePredictUrl, 
            _monitor.PredictionEndpoint, 
            _monitor.ProjectId,
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
            // Initialize item group
            ItemGroupForAIDto response = new ItemGroupForAIDto()
            {
                NewLibraryIdsToTrain = otherItemIds ?? new List<int>()
            };
            
            // Find suitable group for items
            // Build item spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == rootItemId);
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemAuthors)
                    .ThenInclude(lia => lia.Author)
                .Include(li => li.LibraryItemGroup!));
            // Retrieve root item
            var rootItemValue = (await _libraryItemService.GetWithSpecAsync(baseSpec)).Data as LibraryItemDto;
            if (rootItemValue is null)
            {
                // Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
            }
            
            // Check whether root item suitable in library group
            var suitableGroupId = await SuitableLibraryGroup(rootItemValue);
            
            // Get root item
            if (rootItemValue.GroupId != null)
            {
                response.TrainingCode = Guid.Parse(rootItemValue.LibraryItemGroup!.AiTrainingCode);
                response.NewLibraryIdsToTrain.Add(rootItemId);
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
                if (otherItemIds != null && otherItemIds.Count()!=0)
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
            var candidateItems = await _libraryItemService.GetAllWithoutAdvancedSpecAsync(candidateItemsSpec);
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

            // Initialize responses
            var response = new List<CheckedGroupResponseDto<string>>();
            var processedItemIds = new HashSet<int>();
            
            // Build training session spec
            var baseSession =
                new BaseSpecification<AITrainingSession>(ts => ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
            // Apply split query
            baseSession.EnableSplitQuery();
            // Apply include
            baseSession.ApplyInclude(q => q.Include(s => s.TrainingDetails));
            // Retrieve session with spec
            var sessionValue = (await _aiTrainingSessionService.GetWithSpecAsync(baseSession)).Data as AITrainingSessionDto;
            // Check exist any training items 
            var isTrainingLibraryItems = sessionValue != null
                ? sessionValue.TrainingDetails.Select(td => td.LibraryItemId).ToList()
                : new List<int>();

            // Retrieve all ungrouped items
            var ungroupedItemSpec = new BaseSpecification<LibraryItem>(li => 
                li.LibraryItemAuthors.Any() && // Must exist at least one author enabling to train image-based detection
                li.CutterNumber != null &&  // Must exist cutter number 
                li.ClassificationNumber != null && // Must exist classification number
                (
                    !li.IsTrained || // Is not in train status
                    (isTrainingLibraryItems.Any() && !isTrainingLibraryItems.Contains(li.LibraryItemId)) // Filter out all training items
                ) &&
                // Filter out category (Magazine, Newspaper, Book reference, Digital book)
                (
                    li.Category.EnglishName != nameof(LibraryItemCategory.Magazine) &&
                    li.Category.EnglishName != nameof(LibraryItemCategory.Newspaper) &&
                    li.Category.EnglishName != nameof(LibraryItemCategory.ReferenceBook) &&
                    li.Category.EnglishName != nameof(LibraryItemCategory.DigitalBook)
                )
            );
            // Apply split query
            ungroupedItemSpec.EnableSplitQuery();
            // Apply include
            ungroupedItemSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author)
                .Include(li => li.Category)
            );
            // Retrieve with spec
            var ungroupedItemsValue = (await _libraryItemService.GetAllWithoutAdvancedSpecAsync(ungroupedItemSpec)).Data as List<LibraryItemDto>;
            if (ungroupedItemsValue == null)
            {
                // Data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    data: response);
            }

            // Iterate each item to group item sharing the unique features (Title, Subtitle, DDC, Cutter number)
            foreach (var libraryItem in ungroupedItemsValue)
            {
                // Skip processed items
                if (processedItemIds.Contains(libraryItem.LibraryItemId))
                {
                    continue;
                }
                
                // Mark item as processed
                processedItemIds.Add(libraryItem.LibraryItemId);
                
                // Try to extract author's fullname
                var mainAuthor = libraryItem.LibraryItemAuthors
                    .First(x => x.LibraryItemId == libraryItem.LibraryItemId).Author.FullName;

                // Create list of checked properties compare to root item
                var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
                
                // Build up unique features to seek out for other groupable items (mark current item as root)
                var listPropertiesChecked = new List<CheckedGroupDetailDto<string>>
                {
                    new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                        {
                            { nameof(libraryItem.CutterNumber), (int)FieldGroupCheckedStatus.GroupSuccess },
                            {
                                nameof(libraryItem.ClassificationNumber), (int)FieldGroupCheckedStatus.GroupSuccess
                            },
                            { nameof(Author), (int)FieldGroupCheckedStatus.GroupSuccess },
                            { nameof(libraryItem.Title), (int)FieldGroupCheckedStatus.GroupSuccess },
                            { nameof(libraryItem.SubTitle), (int)FieldGroupCheckedStatus.GroupSuccess }
                        },
                        Item = libraryItem,
                        IsRoot = true
                    }
                };
                
                // Find out for other items sharing same unique feature
                var candidateItems = ungroupedItemsValue.Where(
                    li => li.CutterNumber!.Equals(libraryItem.CutterNumber) &&
                          li.ClassificationNumber!.Equals(libraryItem.ClassificationNumber) &&
                          li.LibraryItemAuthors.Any(lia => lia.Author.FullName.Equals(mainAuthor)) &&
                          li.LibraryItemId != libraryItem.LibraryItemId
                ).ToList();

                // Check whether exist any candidate items
                if (!candidateItems.Any()) // Not found
                {
                    int groupStatus = listPropertiesChecked.Any(item => 
                        isTrainingLibraryItems.Contains(item.Item.LibraryItemId)) ? 1 : 0;
                    
                    // Initialize response data (only exist 1 item in group)
                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = (int)FieldGroupCheckedStatus.GroupSuccess,
                        ListCheckedGroupDetail = listPropertiesChecked,
                        GroupStatus = groupStatus
                    };
                    // Add to response
                    response.Add(responseData);
                    // Skip to next item
                    continue;
                }
                else // Found candidate items
                {
                    // Filter out all processed item in candidate items
                    candidateItems.RemoveAll(item => processedItemIds.Contains(item.LibraryItemId));

                    // Iterate each candidate items to add for property match level
                    foreach (var libraryItemDto in candidateItems)
                    {
                        // Initialize check group detail
                        var itemCheckedResult = new CheckedGroupDetailDto<string>()
                        {
                            PropertiesChecked = new Dictionary<string, int>()
                        };

                        // Compare title
                        var titleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(libraryItemDto.Title),
                            StringUtils.RemoveSpecialCharacter(libraryItem.Title));

                        // Compare subtitle
                        int subTitleStatus;
                        // Only process check subtitle for category is not book series
                        if (libraryItem.Category.EnglishName != nameof(LibraryItemCategory.BookSeries)) 
                        {
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
                        }
                        else
                        {
                            // Mark as able to group
                            subTitleStatus = (int)FieldGroupCheckedStatus.AbleToForceGrouped;
                        }
                        
                        // Check whether subtitle is null
                        var isSubTitleNull = string.IsNullOrEmpty(libraryItem.SubTitle);

                        // Compare subtitle status
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

                    // Determine overall status
                    var overallStatus = DetermineOverallStatus(determineOverallStatus);

                    int groupStatus = listPropertiesChecked.Any(item => 
                        isTrainingLibraryItems.Contains(item.Item.LibraryItemId)) ? 1 : 0;
                    
                    // Initialize response data
                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = overallStatus,
                        ListCheckedGroupDetail = listPropertiesChecked,
                        GroupStatus = groupStatus
                    };
                    
                    // Add to response
                    response.Add(responseData);
                }
            }
            // If there is group of BookSeries and have <5 items, remove it from response
            response = response.Where(group => 
                !(group.ListCheckedGroupDetail.Any(detail => 
                    detail.Item.Category.EnglishName == nameof(LibraryItemCategory.BookSeries) &&
                    group.ListCheckedGroupDetail.Count < 5))
            ).ToList();
            
            return new ServiceResult(
                resultCode: ResultCodeConst.AIService_Success0005,
                message: await _msgService.GetMessageAsync(ResultCodeConst.AIService_Success0005), response);
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            throw new Exception("Error invoke when Get and Grade All Suitable Items For Grouping");
        }
    }

    public async Task<IServiceResult> IsAvailableToTrain()
    {
        // Build spec
        var baseSession =
            new BaseSpecification<AITrainingSession>(ts => ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
        // Apply split query
        baseSession.EnableSplitQuery();
        // Apply include
        baseSession.ApplyInclude(q => q
            .Include(s => s.TrainingDetails)
            .ThenInclude(td => td.TrainingImages));
        // Retrieve data with spec
        if ((await _aiTrainingSessionService.GetWithSpecAsync(baseSession)).Data is AITrainingSessionDto sessionValue)
        {
            // Msg: There is existing AI training session
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), sessionValue);
        }

        // Msg: Is able to train
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
            var availableToTrain = await IsAvailableToTrain();
            if (availableToTrain.ResultCode != ResultCodeConst.AIService_Success0007)
            {
                return availableToTrain;
            }

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

            // Build training session spec
            var baseSession =
                new BaseSpecification<AITrainingSession>(ts => ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
            // Apply split query
            baseSession.EnableSplitQuery();
            // Apply include
            baseSession.ApplyInclude(q => q
                .Include(s => s.TrainingDetails)
                .ThenInclude(td => td.TrainingImages));
            // Retrieve session with spec
            var sessionValue =
                (await _aiTrainingSessionService.GetWithSpecAsync(baseSession)).Data as AITrainingSessionDto;
            if (sessionValue != null)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0009), sessionValue);
            }
            
            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();

            // Iterate each untrained group
            for (int i = 0; i < dto.TrainingData.Count; ++i)
            {
                var untrainedGr = dto.TrainingData[i];

                // Iterate each item in group
                for (int j = 0; j < untrainedGr.ItemsInGroup.Count; ++j)
                {
                    var item = untrainedGr.ItemsInGroup[j];

                    // Check item's category
                    // Build spec
                    var cateSpec = new BaseSpecification<Category>(c =>
                        c.LibraryItems.Any(li => li.LibraryItemId == item.LibraryItemId));
                    // Retrieve with spec
                    if ((await _cateService.GetWithSpecAsync(cateSpec)).Data is CategoryDto itemCateDto)
                    {
                        // Determine category
                        switch (itemCateDto.EnglishName)
                        {
                            case nameof(LibraryItemCategory.SingleBook):
                                // Required at least 5 images
                                if (item.ImageFiles.Count < 4)
                                {
                                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                        key: $"trainingData[{i}].itemsInGroup[{j}]",
                                        // Msg: Required at least 5 images for single book
                                        msg: await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0008));
                                }
                                break;
                            case nameof(LibraryItemCategory.BookSeries):
                                // Required at least 5 items in group to process train book series 
                                if (untrainedGr.ItemsInGroup.Count < 5)
                                {
                                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                        key: $"trainingData[{i}].itemsInGroup[{j}]",
                                        // Msg: Required at least 5 images for single book
                                        msg: await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0010));
                                    
                                    throw new UnprocessableEntityException("Invalid data", customErrs);
                                }
                                
                                // Required at least 1 image
                                // if (item.ImageFiles.Count < 1)
                                // {
                                //     customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                //         key: $"trainingData[{i}].itemsInGroup[{j}]",
                                //         // Msg: Required at least 1 image for book series
                                //         msg: await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0010));
                                // }
                                break;
                        }
                    }
                }
            }
            
            // Check whether invoke any errors
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);
            
            // Merge group from previous stage (input) to db and get code
            var trainingDataDic = new Dictionary<Guid, List<(byte[] FileBytes, string FileName)>>();
            // Initialize dictionary lib -> images to save to history
            var itemWithImages = new Dictionary<int, List<string>>();
            // Iterate each group to process train AI
            for (int i = 0; i < dto.TrainingData.Count; ++i)
            {
                var untrainedGroup = dto.TrainingData[i];
                
                // Check if group exist any items
                if(!untrainedGroup.ItemsInGroup.Any()) continue; // Move to next group
                
                // Add images
                foreach (var item in untrainedGroup.ItemsInGroup)
                {
                    itemWithImages.Add(item.LibraryItemId, item.ImageUrls);
                }
                
                // Retrieve first item in group
                var representItemInGroup = untrainedGroup.ItemsInGroup.First();
                // Extract other items in group
                var otherItemsInGroup = untrainedGroup.ItemsInGroup
                    .Where(x => x.LibraryItemId != representItemInGroup.LibraryItemId).ToList();
                // Extract all other item ids
                var otherItemInGroupIds = otherItemsInGroup.Select(x => x.LibraryItemId)
                    .ToList();
                
                // Get available group or create new one
                var checkGroupRes =
                    await GetAvailableGroup(email, representItemInGroup.LibraryItemId, otherItemInGroupIds);
                if (checkGroupRes.Data is null) return checkGroupRes;
                
                // Parse to ItemGroupForAIDto
                var availableGroupValue = (ItemGroupForAIDto)checkGroupRes.Data!;

                // Extract training code 
                var code = availableGroupValue.TrainingCode;
                
                // Initialize training dictionary
                if (!trainingDataDic.ContainsKey(code))
                {
                    trainingDataDic[code] = new List<(byte[] FileBytes, string FileName)>();
                }
            
                // Extract all image fields
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
            
            await _hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 0,
                    session = string.Empty,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = itemWithImages.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            _ = backgroundTask; // Bảo đảm task chạy tiếp trong background

            return result;
        }
        catch (UnprocessableEntityException)
        {
            throw;
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
        // Define services that use in background task
        using var scope = _service.CreateScope();
        var libraryItemService = scope.ServiceProvider
            .GetRequiredService<ILibraryItemService<LibraryItemDto>>();
        var libraryItemGroupService = scope.ServiceProvider
            .GetRequiredService<ILibraryItemGroupService<LibraryItemGroupDto>>();
        var aiTrainingSessionService = scope.ServiceProvider
            .GetRequiredService<IAITrainingSessionService<AITrainingSessionDto>>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AiHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
        var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CustomVisionSettings>>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        
        // Define monitor value
        var currentAiConfiguration = monitor.CurrentValue;
        int currentSession = 0;
        try
        {
            // Initialize base configuration
            var baseConfig = new BaseConfigurationBackgroudDto
            {
                Client = httpClient,
                Configuration = currentAiConfiguration,
                Logger = logger,
                BaseUrl = string.Format(currentAiConfiguration.BaseAIUrl,
                    currentAiConfiguration.TrainingEndpoint, currentAiConfiguration.ProjectId)
            };
            
            // Get or create new tag
            List<TagDto> tags = await GetTagAsync(baseConfig);
            
            // Create session
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
                // Initialize training detail
                var detail = new AITrainingDetailDto()
                {
                    LibraryItemId = itemId,
                    TrainingImages = new List<AITrainingImageDto>()
                };

                // Extract all images by itemId
                var itemImages = detailParam[itemId];
                // Initialize training images
                var aiTrainingImageDtos = new List<AITrainingImageDto>();
                foreach (var itemImage in itemImages)
                {
                    // Add image 
                    aiTrainingImageDtos.Add(new AITrainingImageDto()
                    {
                        ImageUrl = itemImage,
                    });
                }
        
                // Add book cover
                // Build spec
                var itemSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == itemId);
                var coverImageUrl = (await libraryItemService.GetWithSpecAndSelectorAsync(itemSpec, selector: s => s.CoverImage)).Data as string;
                if (!string.IsNullOrEmpty(coverImageUrl))
                {
                    aiTrainingImageDtos.Add(new AITrainingImageDto()
                    {
                        ImageUrl = coverImageUrl
                    });
                }
                
                // Add training images to training detail
                detail.TrainingImages = aiTrainingImageDtos;
                // Add training detail to session
                initSession.TrainingDetails.Add(detail);
            }

            // Add all session, detail and image to db
            var sessionDto = (await aiTrainingSessionService.CreateAsync(initSession)).Data as AITrainingSessionDto;
            if (sessionDto == null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully. Err when create Session");
                await Task.CompletedTask;
                return;
            }

            // Assign training session
            currentSession = sessionDto.TrainingSessionId;
            // Handle uploading image to tag in ai cloud
            foreach (var (key, value) in trainingDataDic)
            {
                // Try to get or create tag
                TagDto tag = tags.FirstOrDefault(x => x.Name == key.ToString()) 
                             ?? await CreateTagAsync(baseConfig, key);
                
                // Initialize memory streams
                var memoryStreams = new List<(MemoryStream Stream, string FileName)>();

                // Get cover image of current 
                // Build group spec
                var groupBaseSpec = new BaseSpecification<LibraryItemGroup>(lig
                    => lig.AiTrainingCode.Equals(key.ToString()));
                // Apply include
                groupBaseSpec.ApplyInclude(q => q.Include(lig => lig.LibraryItems));
                // Retrieve with spec
                var groupValue = (await libraryItemGroupService.GetWithSpecAsync(groupBaseSpec)).Data as LibraryItemGroupDto;
                if (groupValue == null)
                {
                    await hubContext.Clients.User(email).SendAsync("AIProcessMessage",
                        new { message = "Error retrieving group", key });
                    await Task.CompletedTask;
                    return;
                }

                // Extract all untrained item
                var untrainedItems = groupValue.LibraryItems
                    .Where(li => li.IsTrained == false)
                    .ToList();
                foreach (var groupValueLibraryItem in untrainedItems)
                {
                    // Try to get image
                    var response = await httpClient.GetAsync(groupValueLibraryItem.CoverImage);
                    // Using response.IsSuccessStatusCode to check if the request is successful
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

            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 20,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 20);
            
            // Train the model after adding the images
            var iteration = await TrainProjectAsync(baseConfig);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 30,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 30);
            
            // Invoke error
            if (iteration is null)
            {
                await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
                await aiTrainingSessionService.UpdateSuccessSessionStatus(currentSession, false,
                    "Can not get iteration");
                await Task.CompletedTask;
                return;
            }

            // Wait until the training is completed before publishing
            await WaitForTrainingCompletionAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 40,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 40);
            
            // Unpublish previous iteration if necessary (optional)
            await UnpublishPreviousIterationAsync(baseConfig, iteration.Id);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 50,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 50);
            
            // Publish the new iteration and update appsettings.json
            await PublishIterationAsync(baseConfig, iteration.Id, monitor.CurrentValue.PublishedName!);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 60,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 60);
            
            // Process update training status list
            await libraryItemService.UpdateTrainingStatusAsync(detailParam.Keys.ToList());
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 80,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Process update percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 80);
            // Update session status
            sessionDto.TrainingStatus = AITrainingStatus.Completed;
            // Process update session status
            await aiTrainingSessionService.UpdateSuccessSessionStatus(currentSession, true);
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 90,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            //Send notification when finish
            await hubContext.Clients.User(email).SendAsync(
                "AIProcessMessage", new
                {
                    message = 100,
                    session = initSession.TrainingSessionId,
                    NumberOfTrainingGroup = trainingDataDic.Keys.Count,
                    NumberOfTrainingItems = detailParam.Keys.Count,
                    NumberOfTrainingImages = trainingDataDic.Values.SelectMany(x => x).Count()
                }
            );
            
            // Update session percentage
            await aiTrainingSessionService.UpdatePercentage(currentSession, 100);
        }
        catch (Exception ex)
        {
            await hubContext.Clients.User(email).SendAsync("Trained Unsuccessfully");
            if (currentSession != 0)
            {
                await aiTrainingSessionService.UpdateSuccessSessionStatus(currentSession, false,
                    "Can not get iteration");
            }

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
                    var baseSession =
                        new BaseSpecification<AITrainingSession>(ts =>
                            ts.TrainingStatus.Equals(AITrainingStatus.InProgress));
                    var session = await aiTrainingSessionService.GetWithSpecAsync(baseSession);

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
                await aiTrainingSessionService.UpdateSuccessSessionStatus(sessionEntity.TrainingSessionId, false);
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
            await aiTrainingSessionService.UpdateSuccessSessionStatus(sessionEntity.TrainingSessionId, true);
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
        return (titleStatus, subTitleStatus) switch
        {
            // Failed + Failed -> Failed
            ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
                FieldGroupCheckedStatus.GroupFailed),
            // Failed + Success -> Success
            ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
                FieldGroupCheckedStatus.GroupSuccess),
            // Failed + Able to force -> Able to force
            ((int)(FieldGroupCheckedStatus.GroupFailed), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
            // Success + Failed -> Failed
            ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupFailed)) => (int)(
                FieldGroupCheckedStatus.GroupFailed),
            // Able to force + Failed -> Failed
            ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupFailed)) =>
                (int)(FieldGroupCheckedStatus.GroupFailed),
            // Able to force + Able to force -> Able to force
            ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.AbleToForceGrouped))
                => (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
            // Able to force + Success -> Able to force
            ((int)(FieldGroupCheckedStatus.AbleToForceGrouped), (int)(FieldGroupCheckedStatus.GroupSuccess)) =>
                (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
            // Success + Able to force -> Able to force
            ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.AbleToForceGrouped)) =>
                (int)(FieldGroupCheckedStatus.AbleToForceGrouped),
            // Success + Success -> Success
            ((int)(FieldGroupCheckedStatus.GroupSuccess), (int)(FieldGroupCheckedStatus.GroupSuccess)) => (int)(
                FieldGroupCheckedStatus.GroupSuccess),
            // Default -> Failed
            _ => (int)(FieldGroupCheckedStatus.GroupFailed)
        };
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

    private async Task<int> SuitableLibraryGroup(LibraryItemDto libraryItemValue)
    {
        try
        {
            // Check not exist author
            if (!libraryItemValue.LibraryItemAuthors.Any()) return 0;
            
            // Extract main author
            var mainAuthor = libraryItemValue.LibraryItemAuthors.First().Author.FullName;

            // Build group spec
            var groupSpec = new BaseSpecification<LibraryItemGroup>(lig =>
                lig.CutterNumber == libraryItemValue.CutterNumber &&
                lig.ClassificationNumber == libraryItemValue.ClassificationNumber &&
                lig.Author.Equals(mainAuthor));
            // Retrieve all potential group
            var potentialGroupList = (await _libraryItemGroupService.GetAllWithSpecAsync(groupSpec)).Data as List<LibraryItemGroupDto>;
            if (potentialGroupList == null || potentialGroupList.Count == 0) return 0;
            
            int bestGroupId = 0;
            double bestScore = 0;
            // Iterate each group
            foreach (var group in potentialGroupList)
            {
                // Calculate title and subtitle score
                var titleScore = StringUtils.CombinedFuzzinessScore(libraryItemValue.Title, group.Title);
                var subTitleScore = libraryItemValue.SubTitle != null && group.SubTitle != null
                    ? StringUtils.CombinedFuzzinessScore(libraryItemValue.SubTitle, group.SubTitle)
                    : 0;

                // Check status for title and subtitle
                var titleStatus = CompareFieldStatus(libraryItemValue.Title, group.Title);
                int subTitleStatus;
                if (string.IsNullOrEmpty(libraryItemValue.SubTitle) &&
                    string.IsNullOrEmpty(group.SubTitle))
                {
                    subTitleStatus = titleStatus;
                }
                else
                {
                    // Set default as groupable 
                    subTitleStatus = (int) FieldGroupCheckedStatus.AbleToForceGrouped;
                    
                    // Check whether check subtitle for book series
                    var category = libraryItemValue.Category.EnglishName;
                    if (category != nameof(LibraryItemCategory.BookSeries))
                    {
                        // Process check subtitle 
                        subTitleStatus = CompareFieldStatus(
                            StringUtils.RemoveSpecialCharacter(libraryItemValue.SubTitle ?? ""),
                            StringUtils.RemoveSpecialCharacter(group.SubTitle ?? ""));
                    }
                }

                // Combine title and subtitle status
                var combinedStatus =
                    CombineTitleSubTitleStatus(titleStatus, subTitleStatus, group.SubTitle == null);
                
                // Determine group score based on status
                double groupScore = combinedStatus switch
                {
                    (int)FieldGroupCheckedStatus.GroupSuccess => 100,
                    (int)FieldGroupCheckedStatus.AbleToForceGrouped => 50,
                    _ => 0
                };
                
                // Found groupable
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

        var allItem = await _libraryItemService.GetAllWithoutAdvancedSpecAsync(recommendBookBaseSpec);
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
                    var matchRate = Math.Ceiling((double)matchCount / totalObject * 100);

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    // mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
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
                    var compareResultValue = (List<MatchResultDto>)compareResult.Data!;
                    var ocrPoint = compareResultValue.First().TotalPoint;
                    itemTotalPoint.Add(groupValueLibraryItem.LibraryItemId, matchRate * 0.5 * 100 + ocrPoint * 0.5);
                    itemOrcMatchResult.Add(groupValueLibraryItem.LibraryItemId,
                        compareResultValue.First().ToMinimisedMatchResultDto());
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
                    // mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
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

            var allItem = await _libraryItemService.GetAllWithoutAdvancedSpecAsync(recommendBookBaseSpec);
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
        // remember those to generate code
        if (currentBook.Isbn!.Equals(otherBook.Isbn)) score++;
        if (currentBook.PublicationYear == otherBook.PublicationYear) score++;
        if (currentBook.Ean == otherBook.Ean) score++;
        if (currentBook.Summary == otherBook.Summary) score++;
        if (currentBook.EstimatedPrice == otherBook.EstimatedPrice) score++;
        if (currentBook.PageCount == otherBook.PageCount) score++;
        if (currentBook.PhysicalDetails == otherBook.PhysicalDetails) score++;
        if (currentBook.Dimensions == otherBook.Dimensions) score++;
        if (currentBook.AccompanyingMaterial == otherBook.AccompanyingMaterial) score++;
        if (currentBook.GeneralNote == otherBook.GeneralNote) score++;
        if (currentBook.BibliographicalNote == otherBook.BibliographicalNote) score++;
        if (currentBook.AdditionalAuthors == otherBook.AdditionalAuthors) score++;


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
                { Name = "CutterNumber", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },

            //create more field to compare base on that i told you to remember
            new MatchedProperties
                { Name = "Isbn", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "PublicationYear", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "Ean", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "Summary", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "EstimatedPrice", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "PageCount", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "PhysicalDetails", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "Dimensions", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "AccompanyingMaterial", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "BibliographicalNote", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "AdditionalAuthors", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber },
            new MatchedProperties
                { Name = "GeneralNote", IsMatched = currentBook.CutterNumber == otherBook.CutterNumber }
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
                    var matchRate = Math.Round((double)matchCount / totalObject);

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    // mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
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
                    var matchRate = Math.Round((double)matchCount / totalObject * 100);

                    // ocr check
                    List<string> mainAuthor = new List<string>();
                    // mainAuthor.Add(groupValueLibraryItem.GeneralNote!);
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
                        // GeneralNote = groupValueLibraryItem.GeneralNote
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
                // mainAuthor.Add(item.GeneralNote!);
                mainAuthor.Add(item.LibraryItemAuthors.First(x
                        => x.LibraryItemId == item.LibraryItemId)!
                    .Author.FullName);

                var coverImage = _httpClient.GetAsync(item.CoverImage).Result;
                var ocrCheck = new CheckedItemDto()
                {
                    Title = item.Title,
                    SubTitle = item.SubTitle,
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
                itemTotalPoint.Add(item.LibraryItemId, matchObjectPoint[item.LibraryItemId] * 0.5 + ocrPoint * 0.5);
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
            if (bestItemPredictResponse.OCRResult.TotalPoint <
                bestItemPredictResponse.OCRResult.ConfidenceThreshold)
            {
                return new ServiceResult(ResultCodeConst.AIService_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.AIService_Warning0004));
            }

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
            // Initialize url
            var getTagUrl = dto.BaseUrl + "/tags";
            // Add request header
            dto.Client.DefaultRequestHeaders.Add("Training-Key", dto.Configuration.TrainingKey);
            // Process call to specific route
            var response = await dto.Client.GetAsync(getTagUrl);
            // Throws an exception if !IsSuccessStatusCode
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

    private void UpdateTrainingSessionStatus(IAITrainingSessionService<AITrainingSessionDto> trainingSessionService,
        AITrainingSessionDto sessionDto, string errorMessage)
    {
        sessionDto.ErrorMessage = errorMessage;
        sessionDto.TrainingStatus = AITrainingStatus.Failed;
        trainingSessionService.UpdateAsync(sessionDto.TrainingSessionId, sessionDto);
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