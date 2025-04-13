using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
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
    private readonly Lazy<IEmployeeService<EmployeeDto>> _employeeSvc;

    public LibraryItemGroupService(
        // Lazy services
        Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,
        Lazy<IEmployeeService<EmployeeDto>> employeeSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _libItemSvc = libItemSvc;
        _employeeSvc = employeeSvc;
    }

    public async Task<IServiceResult> CreateAsync(LibraryItemGroupDto dto, string createdByEmail)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Check exist created by 
            var isEmailExist = (await _employeeSvc.Value.AnyAsync(e => e.Email == createdByEmail)).Data is true;
            if (!isEmailExist) throw new ForbiddenException("Not allow to access");

            // Check whether group information has existed
            var groupSpec = new BaseSpecification<LibraryItemGroup>(g =>
                Equals(g.ClassificationNumber, dto.ClassificationNumber) && 
                Equals(g.CutterNumber, dto.CutterNumber) &&
                Equals(g.Author, dto.Author)
            );
            // Retrieve group with spec
            var entities = (await _unitOfWork.Repository<LibraryItemGroup, int>().GetAllWithSpecAsync(groupSpec)).ToList();
            if (entities.Any())
            {
                foreach (var group in entities)
                {
                    // Compare title
                    var compareTitleRes = CompareFieldStatus(group.Title, dto.Title);
                    if (compareTitleRes != (int)FieldGroupCheckedStatus.GroupFailed)
                    {
                        // Msg: Unable to create a new group because the document may be grouped with existing groups. Please check again
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0031,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0031));
                    }
                }
            }
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Add necessary fields
            dto.CreatedAt = currentLocalDateTime;
            dto.CreatedBy = createdByEmail;
            dto.AiTrainingCode = Guid.NewGuid().ToString();
            // Process add entity
            await _unitOfWork.Repository<LibraryItemGroup, int>().AddAsync(_mapper.Map<LibraryItemGroup>(dto));
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
        catch (ForbiddenException)
        {
            throw;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);  
            throw new Exception("Error invoke when process create new group");
        }
    }
    
    public async Task<IServiceResult> GetAllPotentialGroupAsync(
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
            
            // Initialize response
            var potentialItemGroups = new List<GetItemPotentialGroup>();

            // Build spec to retrieve for appropriate group
            var groupSpec = new BaseSpecification<LibraryItemGroup>(g => 
                // Match author property
                Equals(g.Author, authorName) &&
                // Match cutter number
                Equals(g.CutterNumber, cutterNumber) &&
                // Match classification
                Equals(g.ClassificationNumber, classificationNumber) &&
                // Is belongs to book series
                (!g.LibraryItems.Any() || g.LibraryItems.All(li => li.Category.EnglishName.Equals(nameof(LibraryItemCategory.BookSeries))))
            );
            // Enable split query
            groupSpec.EnableSplitQuery();
            // Apply include
            groupSpec.ApplyInclude(q => q
                .Include(g => g.LibraryItems)
                    .ThenInclude(li => li.LibraryItemAuthors)
                        .ThenInclude(lia => lia.Author)
                .Include(g => g.LibraryItems)
                    .ThenInclude(li => li.Category)
            );
            // Apply order
            groupSpec.AddOrderByDescending(q => q.CreatedAt);
            
            // Retrieve with spec
            var groupableList = (await _unitOfWork.Repository<LibraryItemGroup, int>()
                .GetAllWithSpecAsync(groupSpec)).ToList();
            if (!groupableList.Any())
            {
                // Data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    data: potentialItemGroups);
            }
            
            // Map to dto collection
            var groupableDtoList = _mapper.Map<List<LibraryItemGroupDto>>(groupableList);
            // Iterate each item to group item sharing the unique features (Title, Subtitle, DDC, Cutter number)
            foreach (var group in groupableDtoList)
            {
                // Extract list of items
                var groupItems = group.LibraryItems.ToList();
                // Create list of checked properties compare to root item
                var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();
                
                foreach (var item in groupItems)
                {
                    // Try to extract author's fullname
                    var mainAuthor = item.LibraryItemAuthors
                        .First(x => x.LibraryItemId == item.LibraryItemId).Author.FullName;
                    
                    // Initialize check group detail
                    var itemCheckedResult = new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                    };
            
                    // Compare title
                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(title),
                        StringUtils.RemoveSpecialCharacter(item.Title));
                    
                    // Check whether subtitle is null
                    var isSubTitleNull = string.IsNullOrEmpty(item.SubTitle);
                    // Set default subTitle status
                    var subTitleStatus = (int)FieldGroupCheckedStatus.AbleToForceGrouped;
                    
                    // Compare subtitle status
                    var titleSubTitleStatus =
                        CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
                    if (titleSubTitleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                    {
                        // Cutter number
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.CutterNumber),
                            (int)FieldGroupCheckedStatus.GroupSuccess);
                        // Classification number
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.ClassificationNumber),
                            (int)FieldGroupCheckedStatus.GroupSuccess);
                        // Author
                        itemCheckedResult.PropertiesChecked.Add(nameof(Author),
                            (int)FieldGroupCheckedStatus.GroupSuccess);
                        
                        // Title, subtitle status
                        itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                        // Title
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.Title), titleStatus);
                        // Subtitle
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.SubTitle), subTitleStatus);
                        // Assign item dto to groupedItemsList
                        itemCheckedResult.Item = item;
                        itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                        // Add item checked result to overall status
                        determineOverallStatus.Add(itemCheckedResult);
                        
                    }
                }
                
                // Determine overall status
                var overallStatus = DetermineOverallStatus(determineOverallStatus);
                
                // Clear all items in group
                group.LibraryItems.Clear();
                // Add to groupedItemsList
                potentialItemGroups.Add(new()
                {
                    GroupDetail = group,
                    CheckResponse = determineOverallStatus.Count > 0
                        ? new CheckedGroupResponseDto<string>()
                        {
                            IsAbleToCreateGroup = overallStatus,
                            ListCheckedGroupDetail = determineOverallStatus,
                            GroupStatus = 0
                        }
                        : null!
                });
            }

            if (potentialItemGroups.Any())
            {
                // Count total actual item
                var totalActualItem = potentialItemGroups.Count;
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
                    potentialItemGroups = potentialItemGroups
                        .Where(g => 
                            g.GroupDetail.Title.Contains(groupSpecification.Search))
                        .ToList();
                }
                
                // Apply pagination
                potentialItemGroups = potentialItemGroups
                    .Skip(groupSpecification.PageSize * (groupSpecification.PageIndex - 1))
                    .Take(groupSpecification.PageSize)
                    .ToList();
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<GetItemPotentialGroup>(
                    potentialItemGroups,
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

    public async Task<IServiceResult> GetAllPotentialGroupByLibraryItemIdAsync(
        ISpecification<LibraryItemGroup> spec, int libraryItemId)
    {
        try
        {
            // Determine current lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Try to parse specification to LibraryItemSpecification
            var groupSpecification = spec as LibraryItemGroupSpecification;
            // Check if specification is null
            if (groupSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Check exist library item
            var itemSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == libraryItemId);
            // Apply include
            itemSpec.ApplyInclude(q => q
                .Include(li => li.Category)
                .Include(li => li.LibraryItemAuthors)
                .ThenInclude(li => li.Author)
            );
            var libItemDto = (await _libItemSvc.Value.GetWithSpecAsync(itemSpec)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
            }
            else if (!libItemDto.LibraryItemAuthors.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng
                        ? "Unable to create library item group as author has not been added"
                        : "Không thể tạo nhóm vì tác giả của tài liệu chưa được xác định");
            }
            else if (string.IsNullOrEmpty(libItemDto.CutterNumber))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng
                        ? "Unable to create library item group as cutter number has not been determined"
                        : "Không thể tạo nhóm vì ký hiệu xếp giá của tài liệu chưa được xác định");
            }
            else if (string.IsNullOrEmpty(libItemDto.ClassificationNumber))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    isEng
                        ? "Unable to create library item group as DDC has not been determined"
                        : "Không thể tạo nhóm vì DDC của tài liệu chưa được xác định");
            }
            else if (libItemDto.Category.EnglishName != nameof(LibraryItemCategory.BookSeries))
            {
                // Msg: Item category is not belong to book series to process add to group items
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0029,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0029));
            }
            else if (libItemDto.GroupId != null)
            {
                // Msg: Unable to progress {0} as {1} already exist
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0003,
                    message: StringUtils.Format(errMsg, args:
                    [
                        isEng ? "create new group" : "tạo nhóm tài liệu mới",
                        isEng ? "group item" : "nhóm tài liệu"
                    ]));
            }

            // Initialize response
            var potentialItemGroups = new List<GetItemPotentialGroup>();

            // Retrieve main author
            // Build spec to retrieve for appropriate group
            var groupSpec = new BaseSpecification<LibraryItemGroup>(g =>
                // Match author property
                Equals(g.Author, libItemDto.LibraryItemAuthors.First().Author.FullName) &&
                // Match cutter number
                Equals(g.CutterNumber, libItemDto.CutterNumber) &&
                // Match classification
                Equals(g.ClassificationNumber, libItemDto.ClassificationNumber) &&
                // Is belongs to book series
                (!g.LibraryItems.Any() || g.LibraryItems.All(li =>
                    li.Category.EnglishName.Equals(nameof(LibraryItemCategory.BookSeries))))
            );
            // Enable split query
            groupSpec.EnableSplitQuery();
            // Apply include
            groupSpec.ApplyInclude(q => q
                .Include(g => g.LibraryItems)
                .ThenInclude(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author)
                .Include(g => g.LibraryItems)
                .ThenInclude(li => li.Category)
            );
            // Apply order
            groupSpec.AddOrderByDescending(q => q.CreatedAt);

            // Retrieve with spec
            var groupableList = (await _unitOfWork.Repository<LibraryItemGroup, int>()
                .GetAllWithSpecAsync(groupSpec)).ToList();
            if (!groupableList.Any())
            {
                // Data not found or empty
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0004,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    data: potentialItemGroups);
            }

            // Map to dto collection
            var groupableDtoList = _mapper.Map<List<LibraryItemGroupDto>>(groupableList);
            // Iterate each item to group item sharing the unique features (Title, Subtitle, DDC, Cutter number)
            foreach (var group in groupableDtoList)
            {
                // Extract list of items
                var groupItems = group.LibraryItems.ToList();
                // Create list of checked properties compare to root item
                var determineOverallStatus = new List<CheckedGroupDetailDto<string>>();

                foreach (var item in groupItems)
                {
                    // Initialize check group detail
                    var itemCheckedResult = new CheckedGroupDetailDto<string>()
                    {
                        PropertiesChecked = new Dictionary<string, int>()
                    };

                    // Compare title
                    var titleStatus = CompareFieldStatus(
                        StringUtils.RemoveSpecialCharacter(libItemDto.Title),
                        StringUtils.RemoveSpecialCharacter(item.Title));

                    // Check whether subtitle is null
                    var isSubTitleNull = string.IsNullOrEmpty(item.SubTitle);
                    // Set default subTitle status
                    var subTitleStatus = (int)FieldGroupCheckedStatus.AbleToForceGrouped;

                    // Compare subtitle status
                    var titleSubTitleStatus =
                        CombineTitleSubTitleStatus(titleStatus, subTitleStatus, isSubTitleNull);
                    if (titleSubTitleStatus != (int)FieldGroupCheckedStatus.GroupFailed)
                    {
                        // Cutter number
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.CutterNumber),
                            (int)FieldGroupCheckedStatus.GroupSuccess);
                        // Classification number
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.ClassificationNumber),
                            (int)FieldGroupCheckedStatus.GroupSuccess);
                        // Author
                        itemCheckedResult.PropertiesChecked.Add(nameof(Author),
                            (int)FieldGroupCheckedStatus.GroupSuccess);

                        // Title, subtitle status
                        itemCheckedResult.PropertiesChecked.Add("TitleSubTitleStatus", titleSubTitleStatus);
                        // Title
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.Title), titleStatus);
                        // Subtitle
                        itemCheckedResult.PropertiesChecked.Add(nameof(item.SubTitle), subTitleStatus);
                        // Assign item dto to groupedItemsList
                        itemCheckedResult.Item = item;
                        itemCheckedResult.PropertiesChecked.Remove("TitleSubTitleStatus");
                        // Add item checked result to overall status
                        determineOverallStatus.Add(itemCheckedResult);

                    }
                }

                // Determine overall status
                var overallStatus = DetermineOverallStatus(determineOverallStatus);

                // Clear all items in group
                group.LibraryItems.Clear();
                // Add to groupedItemsList
                potentialItemGroups.Add(new()
                {
                    GroupDetail = group,
                    CheckResponse = determineOverallStatus.Count > 0
                        ? new CheckedGroupResponseDto<string>()
                        {
                            IsAbleToCreateGroup = overallStatus,
                            ListCheckedGroupDetail = determineOverallStatus,
                            GroupStatus = 0
                        }
                        : null!
                });
            }

            if (potentialItemGroups.Any())
            {
                // Count total actual item
                var totalActualItem = potentialItemGroups.Count;
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
                    potentialItemGroups = potentialItemGroups
                        .Where(g =>
                            g.GroupDetail.Title.Contains(groupSpecification.Search))
                        .ToList();
                }

                // Apply pagination
                potentialItemGroups = potentialItemGroups
                    .Skip(groupSpecification.PageSize * (groupSpecification.PageIndex - 1))
                    .Take(groupSpecification.PageSize)
                    .ToList();

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<GetItemPotentialGroup>(
                    potentialItemGroups,
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