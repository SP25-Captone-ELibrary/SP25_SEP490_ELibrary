using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemGroupService : GenericService<LibraryItemGroup, LibraryItemGroupDto, int>,
    ILibraryItemGroupService<LibraryItemGroupDto>
{
    private readonly Lazy<ILibraryItemService<LibraryItemDto>> _libItemSvc;

    public LibraryItemGroupService(
        // Lazy services
        Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
    : base(msgService, unitOfWork, mapper, logger)
    {
        _libItemSvc = libItemSvc;
    }

    public async Task<IServiceResult> CreateForItemAsync(int libraryItemId, string createdByEmail)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Determine current lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist library item
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == libraryItemId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author)
                .Include(q => q.Category)
            );
            // Retrieve with spec
            var libItem = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (libItem == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002, 
                    message: StringUtils.Format(errMsg, isEng ? "library item to add group" : "tài liệu cần thêm nhóm"));
            }
            else if (!libItem.LibraryItemAuthors.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng 
                        ? "Unable to create library item group as author has not been added" 
                        : "Không thể tạo nhóm vì tác giả của tài liệu chưa được xác định");
            }
            else if (string.IsNullOrEmpty(libItem.CutterNumber))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng 
                        ? "Unable to create library item group as cutter number has not been determined" 
                        : "Không thể tạo nhóm vì ký hiệu xếp giá của tài liệu chưa được xác định");
            }
            else if (string.IsNullOrEmpty(libItem.ClassificationNumber))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng 
                        ? "Unable to create library item group as DDC has not been determined" 
                        : "Không thể tạo nhóm vì DDC của tài liệu chưa được xác định");
            }
            else if (libItem.Category.EnglishName != nameof(LibraryItemCategory.BookSeries))
            {
                // Msg: Item category is not belong to book series to process add to group items
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0029,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0029));
            }
            
            // Check exist group
            var groupSpec = new BaseSpecification<LibraryItemGroup>(g => 
                libItem.LibraryItemAuthors.Any(lia => Equals(lia.Author.FullName, g.Author)) &&
                libItem.CutterNumber != null && Equals(libItem.CutterNumber, g.CutterNumber) &&
                libItem.ClassificationNumber != null && Equals(libItem.ClassificationNumber, g.ClassificationNumber) &&
                libItem.Category.EnglishName.Equals(nameof(LibraryItemCategory.BookSeries))
            );
            // Retrieve all groupable
            var groupableList = (await _unitOfWork.Repository<LibraryItemGroup, int>().GetAllWithSpecAsync(groupSpec)).ToList();
            if (groupableList.Any())
            {
                // Try to check whether exist any title that groupable
                foreach (var group in groupableList)
                {
                    // Compare title
                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(libItem.Title),
                        StringUtils.RemoveSpecialCharacter(group.Title));

                    if (titleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                    {
                        // Msg: Unable to create a new group because the document may be grouped with existing groups. Please check again
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0031,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0031));
                    }
                }
            }
            
            // Initialize group details
            var mainAuthor = libItem.LibraryItemAuthors.First().Author.FullName;
            var newGroupDto = new LibraryItemGroupDto()
            {
                Author = mainAuthor,
                Title = libItem.Title,
                SubTitle = libItem.SubTitle,
                CutterNumber = libItem.CutterNumber,
                ClassificationNumber = libItem.ClassificationNumber,
                CreatedAt = currentLocalDateTime,
                CreatedBy = createdByEmail,
                TopicalTerms = libItem.TopicalTerms,
                AiTrainingCode = Guid.NewGuid().ToString()
            };
            
            // Process create new group
            await _unitOfWork.Repository<LibraryItemGroup, int>().AddAsync(_mapper.Map<LibraryItemGroup>(newGroupDto));
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Create success
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
            }
            
            // Failed to create
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create group for item");
        }
    }
    
    // TODO: Fix return groupable id not return all groupable items
    public async Task<IServiceResult> GetAllItemPotentialGroupAsync(
        ISpecification<LibraryItemGroup> spec,
        string title, string cutterNumber, 
        string classificationNumber, string authorName)
    {
        try
        {
            // Try to parse specification to LibraryItemSpecification
            var groupSpecification = spec as LibraryItemGroupSpecification;
            // Check if specification is null
            if (groupSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Initialize responses
            var groupedItemsList = new List<CheckedGroupResponseDto<string>>();
            var processedItemIds = new HashSet<int>();
            
            // Build spec to retrieve for appropriate group
            var unGroupItemSpec = new BaseSpecification<LibraryItem>(li => 
                li.LibraryItemAuthors.Any(lia => Equals(lia.Author.FullName, authorName)) &&
                li.CutterNumber != null && Equals(li.CutterNumber, cutterNumber) &&
                li.ClassificationNumber != null && Equals(li.ClassificationNumber, classificationNumber) &&
                li.Category.EnglishName.Equals(nameof(LibraryItemCategory.BookSeries)) &&
                li.GroupId != null // Has been grouped
            );
            // Enable split query
            unGroupItemSpec.EnableSplitQuery();
            // Apply include
            unGroupItemSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemAuthors)
                    .ThenInclude(lia => lia.Author)
                .Include(li => li.Category)
            );
            // Apply order
            unGroupItemSpec.AddOrderByDescending(q => q.CreatedAt);
            // Initialize valid ungroup items
            var validUngroupItems = new List<LibraryItemDto>();
            // Retrieve with spec
            var ungroupedItemsValue = (await _libItemSvc.Value.GetAllWithoutAdvancedSpecAsync(unGroupItemSpec)).Data as List<LibraryItemDto>;
            if (ungroupedItemsValue == null)
            {
                // Data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    data: groupedItemsList);
            }
            else
            {
                // Try to check whether exist any title that groupable
                foreach (var groupItem in ungroupedItemsValue)
                {
                    // Compare title
                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(title),
                        StringUtils.RemoveSpecialCharacter(groupItem.Title));

                    if (titleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                    {
                        // Add to valid item group
                        validUngroupItems.Add(groupItem);
                    }
                }
            }
            
            // Iterate each item to group item sharing the unique features (Title, Subtitle, DDC, Cutter number)
            foreach (var libraryItem in validUngroupItems)
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
                    li => li.CutterNumber != null && li.CutterNumber.Equals(libraryItem.CutterNumber) &&
                          li.ClassificationNumber != null && li.ClassificationNumber.Equals(libraryItem.ClassificationNumber) &&
                          li.LibraryItemAuthors.Any(lia => lia.Author.FullName.Equals(mainAuthor)) &&
                          li.LibraryItemId != libraryItem.LibraryItemId
                ).ToList();
                
                // Check whether exist any candidate items
                if (!candidateItems.Any()) // Not found
                {
                    // Initialize groupedItemsList data (only exist 1 item in group)
                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = (int)FieldGroupCheckedStatus.GroupSuccess,
                        ListCheckedGroupDetail = listPropertiesChecked,
                        GroupStatus = 0
                    };
                    // Add to groupedItemsList
                    groupedItemsList.Add(responseData);
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

                        // Check whether subtitle is null
                        var isSubTitleNull = string.IsNullOrEmpty(libraryItem.SubTitle);
                        // Set default subTitle status
                        var subTitleStatus = (int)FieldGroupCheckedStatus.AbleToForceGrouped;
                            
                        // Compare subtitle status
                        var titleSubTitleStatus =
                            CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
                        if (titleSubTitleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                        {
                            // Cutter number
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.CutterNumber),
                                (int)FieldGroupCheckedStatus.GroupSuccess);
                            // Classification number
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.ClassificationNumber),
                                (int)FieldGroupCheckedStatus.GroupSuccess);
                            // Author
                            itemCheckedResult.PropertiesChecked.Add(nameof(Author),
                                (int)FieldGroupCheckedStatus.GroupSuccess);
                            // Title, subtitle status
                            itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                            // Add item checked result to overall status
                            determineOverallStatus.Add(itemCheckedResult);
                            
                            // Title
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.Title), titleStatus);
                            // Subtitle
                            itemCheckedResult.PropertiesChecked.Add(nameof(libraryItem.SubTitle), subTitleStatus);
                            // Assign item dto to groupedItemsList
                            itemCheckedResult.Item = libraryItemDto;
                            itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                            
                            // Add item checked result
                            listPropertiesChecked.Add(itemCheckedResult);
                            // Add processed item
                            processedItemIds.Add(libraryItemDto.LibraryItemId);
                        }
                    }
                    
                    // Determine overall status
                    var overallStatus = DetermineOverallStatus(determineOverallStatus);
                    
                    // Initialize groupedItemsList data
                    var responseData = new CheckedGroupResponseDto<string>()
                    {
                        IsAbleToCreateGroup = overallStatus,
                        ListCheckedGroupDetail = listPropertiesChecked,
                        GroupStatus = 0
                    };
                    
                    // Add to groupedItemsList
                    groupedItemsList.Add(responseData);
                }
            }

            if (groupedItemsList.Any())
            {
                // Count total actual item
                var totalActualItem = groupedItemsList.Count;
                // Count total page
                var totalPage = (int)Math.Ceiling((double)totalActualItem / groupSpecification.PageSize);
                
                // Set pagination to specification after count total library item
                if (groupSpecification.PageIndex > totalPage
                    || groupSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
                {
                    groupSpecification.PageIndex = 1; // Set default to first page
                }
                
                // Process search
                if (!string.IsNullOrEmpty(groupSpecification.Search))
                {
                    groupedItemsList = groupedItemsList.Where(g => 
                        g.ListCheckedGroupDetail.Any(li => 
                            li.Item.Title.Contains(groupSpecification.Search))
                    ).ToList();
                }
                
                // Apply pagination
                groupedItemsList = groupedItemsList
                    .Skip(groupSpecification.PageSize * (groupSpecification.PageIndex - 1))
                    .Take(groupSpecification.PageSize)
                    .ToList();
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<CheckedGroupResponseDto<string>>(
                    groupedItemsList,
                    groupSpecification.PageIndex, groupSpecification.PageSize, totalPage, totalActualItem);
                
                // Get data successfully
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                data: new List<CheckedGroupResponseDto<string>>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all item potential group");
        }
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
}