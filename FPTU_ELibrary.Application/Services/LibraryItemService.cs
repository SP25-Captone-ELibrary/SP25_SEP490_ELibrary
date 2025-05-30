using System.Linq.Expressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Dtos.Recommendation;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Elastic.Mappers;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemService : GenericService<LibraryItem, LibraryItemDto, int>,
    ILibraryItemService<LibraryItemDto>
{
    // Configure lazy service
    private readonly Lazy<IElasticService> _elasticService;
    private readonly Lazy<IUserService<UserDto>> _userService;
    private readonly Lazy<IAITrainingSessionService<AITrainingSessionDto>> _aiTrainingSessionSvc;
    private readonly Lazy<IEmployeeService<EmployeeDto>> _employeeService;
    private readonly Lazy<ILibraryCardService<LibraryCardDto>> _cardService;
    private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecordService;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowRequestService;
    private readonly Lazy<ILibraryItemAuthorService<LibraryItemAuthorDto>> _itemAuthorService;
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceService;
    private readonly Lazy<ILibraryItemGroupService<LibraryItemGroupDto>> _itemGroupService;
    private readonly Lazy<ILibraryResourceService<LibraryResourceDto>> _resourceService;
    private readonly Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> _whTrackingService;
    private readonly Lazy<IReservationQueueService<ReservationQueueDto>> _reservationQueueService;

    private readonly IS3Service _s3Service;
    private readonly ICloudinaryService _cloudService;
    private readonly ICategoryService<CategoryDto> _cateService;
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly AppSettings _appSettings;
    
    private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionService;

    public LibraryItemService(
        // Lazy service
        Lazy<IElasticService> elasticService,
        Lazy<IUserService<UserDto>> userService,
        Lazy<IEmployeeService<EmployeeDto>> employeeService,
        Lazy<IAITrainingSessionService<AITrainingSessionDto>> aiTrainingSessionSvc,
        Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecordService,
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowRequestService,
        Lazy<ILibraryCardService<LibraryCardDto>> cardService,
        Lazy<ILibraryItemAuthorService<LibraryItemAuthorDto>> itemAuthorService,
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceService,
        Lazy<ILibraryItemGroupService<LibraryItemGroupDto>> itemGroupService,
        Lazy<ILibraryResourceService<LibraryResourceDto>> resourceService,
        Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> whTrackingService,
        Lazy<IReservationQueueService<ReservationQueueDto>> reservationQueueService,
        // Normal service
        IS3Service s3Service,
        ICloudinaryService cloudService,
        IAuthorService<AuthorDto> authorService,
        ICategoryService<CategoryDto> cateService,
        ILibraryShelfService<LibraryShelfDto> libShelfService,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionService,
        IOptionsMonitor<AppSettings> monitor,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
        _s3Service = s3Service;
        _authorService = authorService;
        _userService = userService;
        _cardService = cardService;
        _employeeService = employeeService;
        _aiTrainingSessionSvc = aiTrainingSessionSvc;
        _borrowRecordService = borrowRecordService;
        _borrowRequestService = borrowRequestService;
        _cloudService = cloudService;
        _cateService = cateService;
        _libShelfService = libShelfService;
        _conditionService = conditionService;
        _elasticService = elasticService;
        _resourceService = resourceService;
        _itemGroupService = itemGroupService;
        _itemInstanceService = itemInstanceService;
        _reservationQueueService = reservationQueueService;
        _itemAuthorService = itemAuthorService;
        _whTrackingService = whTrackingService;
        _appSettings = monitor.CurrentValue;
    }

    public async Task<IServiceResult> CreateAsync(LibraryItemDto dto, int trackingDetailId)
    {
        try
        {
            // Determine current lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Select list of author ids
            var authorIds = dto.LibraryItemAuthors
                .Select(be => be.AuthorId)
                .Distinct() // Eliminate same authorId from many library item
                .ToList();
            // Count total exist result
            var countAuthorResult = await _authorService.CountAsync(
                new BaseSpecification<Author>(ct => authorIds.Contains(ct.AuthorId)));
            // Check exist any author not being counted
            if (int.TryParse(countAuthorResult.Data?.ToString(), out var totalAuthor) // Parse result to integer
                && totalAuthor != authorIds.Count) // Not exist 1-many author
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0001));
            }

            // Custom error responses
            var customErrors = new Dictionary<string, string[]>();
            // Initialize hash set of string to check unique of barcode
            var itemInstanceBarcodes = new HashSet<string>();

            // Check exist category 
            var categoryDto = (await _cateService.GetByIdAsync(dto.CategoryId)).Data as CategoryDto;
            if (categoryDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "category" : "phân loại"));
            }

            // Check whether create with specific group
            if (dto.GroupId != null && dto.GroupId > 0)
            {
                // Check exist group 
                // Build spec
                var groupSpec = new BaseSpecification<LibraryItemGroup>(g => g.GroupId == dto.GroupId);
                // Apply include
                groupSpec.ApplyInclude(q => q
                    .Include(g => g.LibraryItems));
                // Retrieve group by spec
                var groupDto = (await _itemGroupService.Value.GetWithSpecAsync(groupSpec)).Data as LibraryItemGroupDto;
                if (groupDto == null) // not found
                {
                    // Add error 
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(LibraryItem.GroupId)),
                        msg: isEng ? "Group is not exist" : "Không tìm thấy nhóm");
                }
            }

            // Check exist cover image
            if (!string.IsNullOrEmpty(dto.CoverImage))
            {
                // Initialize field
                var isImageOnCloud = true;

                // Extract provider public id
                var publicId = StringUtils.GetPublicIdFromUrl(dto.CoverImage);
                if (publicId != null) // Found
                {
                    // Process check exist on cloud			
                    isImageOnCloud = (await _cloudService.IsExistAsync(publicId, FileType.Image)).Data is true;
                }

                if (!isImageOnCloud || publicId == null) // Not found image or public id
                {
                    // Add error
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key:StringUtils.ToCamelCase(nameof(LibraryItemDto.CoverImage)),
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
                }
            }

            // Iterate each library item instance (if any) to check valid data
            var listItemInstance = dto.LibraryItemInstances.ToList();
            for (int i = 0; i < listItemInstance.Count; ++i)
            {
                var iInstance = listItemInstance[i];

                if (itemInstanceBarcodes.Add(iInstance.Barcode)) // Add to hash set string to ensure uniqueness
                {
                    // Check exist edition copy barcode within DB
                    var isCodeExist = await _unitOfWork.Repository<LibraryItemInstance, int>()
                        .AnyAsync(x => x.Barcode == iInstance.Barcode);
                    if (isCodeExist)
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0005);
                        // Add errors
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                            key: $"libraryItemInstances[{i}].barcode",
                            msg: StringUtils.Format(errMsg, $"'{iInstance.Barcode}'"));
                    }
                    else
                    {
                        // Try to validate with category prefix
                        var isValidBarcode =
                            StringUtils.IsValidBarcodeWithPrefix(iInstance.Barcode, categoryDto.Prefix);
                        if (!isValidBarcode)
                        {
                            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
                            // Add errors
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                                key: $"libraryItemInstances[{i}].barcode",
                                msg: StringUtils.Format(errMsg, $"'{categoryDto.Prefix}'"));
                        }
                        
                        // Try to validate barcode length
                        var barcodeNumLength = iInstance.Barcode.Length - categoryDto.Prefix.Length; 
                        if (barcodeNumLength != _appSettings.InstanceBarcodeNumLength) // Different from threshold value
                        {
                            // Add errors
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                                key: $"libraryItemInstances[{i}].barcode",
                                msg: isEng 
                                    ? $"Total barcode number after prefix must equals to {_appSettings.InstanceBarcodeNumLength}"
                                    : $"Tổng chữ số sau tiền tố phải bằng {_appSettings.InstanceBarcodeNumLength}"
                            );
                        }
                    }
                }
                else // Duplicate found
                {
                    // Add error 
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: $"libraryItemInstances[{i}].barcode",
                        msg: isEng
                            ? $"Barcode '{iInstance.Barcode}' is duplicated"
                            : $"Số đăng ký cá biệt '{iInstance.Barcode}' đã bị trùng"
                    );
                }

                // Default status
                iInstance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                // Boolean 
                iInstance.IsDeleted = false;
                iInstance.IsCirculated = false;
            }

            // Iterate each library resource (if any) to check valid data
            var listResource = dto.LibraryItemResources.Select(lir => lir.LibraryResource).ToList();
            for (int i = 0; i < listResource.Count; ++i)
            {
                var lir = listResource[i];

                // Get file type
                if (Enum.TryParse(typeof(FileType), lir.FileFormat, out var fileType))
                {
                    // Determine file type
                    if ((FileType)fileType == FileType.Image)
                    {
                        // Check exist resource
                        var checkExistResult = await _cloudService.IsExistAsync(lir.ProviderPublicId, (FileType)fileType!);
                        if (checkExistResult.Data is false) // Return when not found resource on cloud
                        {
                            // Add error
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                                key: $"libraryResources[{i}].resourceTitle",
                                msg: await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0003));
                        }
                    }
                    else if ((FileType)fileType == FileType.Video && (await _s3Service.GetFileUrlAsync(AudioResourceType.Original, lir.S3OriginalName!)).Data is string url)
                    {
                        lir.ResourceUrl = url;
                    }
                    
                    // Check not same publicId and resourceUrl
                    var isDuplicateContent = await _unitOfWork.Repository<LibraryResource, int>().AnyAsync(x =>
                        x.ProviderPublicId == lir.ProviderPublicId || // With specific public id
                        x.ResourceUrl == lir.ResourceUrl); // with specific resource url
                    if (isDuplicateContent) // Not allow to have same resource content
                    {
                        // Add error
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                            key: $"libraryResources[{i}].resourceTitle",
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0003));
                    }
                }
            }

            // Default value
            dto.IsTrained = false;
            dto.IsDeleted = false;
            dto.CanBorrow = false;
            // Clear ISBN hyphens
            dto.Isbn = !string.IsNullOrEmpty(dto.Isbn) ? ISBN.CleanIsbn(dto.Isbn) : dto.Isbn;
            // Check exist Isbn
            var isIsbnExist = await _unitOfWork.Repository<LibraryItem, int>()
                .AnyAsync(x => x.Isbn == dto.Isbn);
            if (isIsbnExist && !string.IsNullOrEmpty(dto.Isbn)) // already exist 
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0007);
                // Add error
                customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                    key: StringUtils.ToCamelCase(nameof(LibraryItem.Isbn)),
                    // Isbn already exist message
                    msg: StringUtils.Format(errMsg, dto.Isbn ?? string.Empty));
            }

            // Check exist tracking detail
            var whTrackingDetail = (await _whTrackingService.Value.GetByIdAsync(trackingDetailId)
                ).Data as WarehouseTrackingDetailDto;
            if (whTrackingDetail == null)
            {
                // Add error 
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "thông tin đăng ký nhập kho"));
            }
            else
            {
                // Check match ISBN (only process when item include ISBN)
                if (!string.IsNullOrEmpty(dto.Isbn) && !Equals(whTrackingDetail.Isbn, dto.Isbn))
                {
                    // Add error 
                    // ISBN of selected warehouse tracking detail doesn't match
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TrackingDetailId)), 
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0007));
                }
                
                // Check whether warehouse tracking detail exist ISBN, but not for cataloging item  
                if (string.IsNullOrEmpty(dto.Isbn) && !string.IsNullOrEmpty(whTrackingDetail.Isbn))
                {
                    // Add error
                    // Selected warehouse tracking detail is incorrect, cataloging item need ISBN to continue
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TrackingDetailId)), 
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0008));
                }
                
                // Check whether same category
                if (!Equals(dto.CategoryId, whTrackingDetail.CategoryId)) 
                {
                    // Add error 
                    // Msg: The action cannot be performed as category of item and warehouse tracking detail is different
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(Category.CategoryId)), 
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0011));
                }
                
                // Check whether tracking detail exist in any other item
                var isExistInOtherItem = whTrackingDetail.LibraryItemId != null;
                if (isExistInOtherItem)
                {
                    // Add error 
                    // Msg: Warehouse tracking detail has already been in other item
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TrackingDetailId)), 
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0013));
                }
                
                // Check exist barcode range in warehouse tracking detail
                if (string.IsNullOrEmpty(whTrackingDetail.BarcodeRangeFrom) ||
                    string.IsNullOrEmpty(whTrackingDetail.BarcodeRangeTo))
                {
                    // Msg: Unique barcode range of warehouse tracking detail is not valid. Please modify and try again
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                        key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TrackingDetailId)), 
                        msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0021));
                }
                else
                {
                    // Check whether belongs to range of barcode in warehouse tracking detail
                    var barcodeRangeFrom = StringUtils.ExtractNumber(
                        input: whTrackingDetail.BarcodeRangeFrom,
                        prefix: categoryDto.Prefix,
                        length: _appSettings.InstanceBarcodeNumLength);
                    var barcodeRangeTo = StringUtils.ExtractNumber(
                        input: whTrackingDetail.BarcodeRangeTo,
                        prefix: categoryDto.Prefix,
                        length: _appSettings.InstanceBarcodeNumLength);
                    
                    // Generate range barcodes
                    var barcodes = StringUtils.AutoCompleteBarcode(
                        prefix: categoryDto.Prefix,
                        length: _appSettings.InstanceBarcodeNumLength,
                        min: barcodeRangeFrom,
                        max: barcodeRangeTo);
                    
                    // Add range library item instances
                    foreach (var barcode in barcodes)
                    {
                        // Retrieve condition (if any)
                        var libConditions = dto.LibraryItemInstances.FirstOrDefault(li => Equals(li.Barcode, barcode))?.LibraryItemConditionHistories;
                        
                        dto.LibraryItemInstances.Add(new ()
                        {
                            // Barcode
                            Barcode = barcode,
                            // Default status
                            Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
                            // Condition
                            LibraryItemConditionHistories = libConditions != null && libConditions.Any() // Check exist requested condition
                                ? libConditions // Assign if exist
                                : new List<LibraryItemConditionHistoryDto>() // Initialize & use default if not exist
                                {
                                    new()
                                    {
                                        ConditionId = whTrackingDetail.ConditionId
                                    }
                                }
                        });
                    }
                }
            }
            
            // Any errors invoke when checking valid data
            if (customErrors.Any()) // exist errors
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }
            
            // Process create new item
            var mappingEntity = _mapper.Map<LibraryItem>(dto);
            await _unitOfWork.Repository<LibraryItem, int>().AddAsync(mappingEntity);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0) // Is saved
            {
                // Process update item to warehouse tracking detail
                var isWarehouseUpdated = (await _whTrackingService.Value.UpdateItemFromInternalAsync(
                    trackingDetailId, mappingEntity.LibraryItemId)).Data is true;
                if (!isWarehouseUpdated) // Fail to update item to warehouse tracking 
                {
                    var customMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                    return new ServiceResult(ResultCodeConst.SYS_Success0001,
                        customMsg + (isEng 
                            ? ", but failed to add item to warehouse tracking" 
                            : ", nhưng cập nhật tài liệu vào thông tin nhập kho thất bại"), new CreateLibraryItemResult()
                        {
                            LibraryItemId = mappingEntity.LibraryItemId
                        });
                }
                
                // Save change successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), new CreateLibraryItemResult()
                    {
                        LibraryItemId = mappingEntity.LibraryItemId
                    });
            }

            // Fail to save
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create new library item");
        }
    }

    public async Task<IServiceResult> AddRangeInstancesWithoutSaveChangesAsync(List<LibraryItemDto> itemListIncludeInstances)
    {
        try
        {
            if (itemListIncludeInstances.Any())
            {
                // Retrieve default condition 
                var goodCondition = (await _conditionService.GetWithSpecAsync(
                    new BaseSpecification<LibraryItemCondition>(
                        lc => lc.EnglishName == nameof(LibraryItemConditionStatus.Good)))).Data as LibraryItemConditionDto;
                if (goodCondition == null)
                {
                    // Logging
                    _logger.Error("Good condition could not be found to process add range item instances");
                    // Mark as failed to create
                    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
                }
                
                // Iterate each item list to process add range instances
                foreach (var item in itemListIncludeInstances)
                {
                    // Retrieve item with spec
                    var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == item.LibraryItemId);
                    // Apply include
                    baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemInventory!));
                    var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
                    if (existingEntity == null || !item.LibraryItemInstances.Any())
                    {
                        // Logging
                        _logger.Error("Not found fibrary item to process add range item instances");
                        // Mark as failed to create
                        return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
                    }
                
                    // Add range item instances
                    item.LibraryItemInstances
                        .ToList()
                        // Iterate each request item instances to add to existing entity
                        .ForEach(li => existingEntity.LibraryItemInstances.Add(_mapper.Map<LibraryItemInstance>(li)));
                    // Increase item's inventory total
                    if (existingEntity.LibraryItemInventory == null)
                    {
                        // Initialize new inventory
                        existingEntity.LibraryItemInventory = new()
                        {
                            TotalUnits = existingEntity.LibraryItemInstances.Count
                        };
                    }
                    else
                    {
                        // Update existing inventory
                        existingEntity.LibraryItemInventory.TotalUnits += item.LibraryItemInstances.Count;
                    }
                
                    // Progress update
                    await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(existingEntity);
                }
                
                // Mark as create success
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }
            
            // Logging 
            _logger.Error("Error invoke when process saving range item instances");
            // Mark as failed to create
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add range item instances without save changes");
        }
    }

    public async Task<IServiceResult> AssignItemToGroupAsync(int libraryItemId, int groupId)
    {
        try
        {
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
                .Include(li => li.Category)
            );
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Fail0002, 
                    message: StringUtils.Format(errMsg, isEng ? "library item to add group" : "tài liệu cần thêm nhóm"));
            }
            else if (existingEntity.Category.EnglishName != nameof(LibraryItemCategory.BookSeries))
            {
                // Msg: Item category is not belong to book series to process add to group items
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0029,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0029));
            }
            
            // Check exist group 
            var groupDto = (await _itemGroupService.Value.GetByIdAsync(groupId)).Data as LibraryItemGroupDto;
            if (groupDto == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Warning0002, 
                    message: StringUtils.Format(errMsg, isEng ? "group to add" : "nhóm cho tài liệu"));
            }
        
            // Check whether item properties enabling to group
            var isGroupable = 
            (
                existingEntity.LibraryItemAuthors.Any(lia => Equals(lia.Author.FullName, groupDto.Author)) &&
                existingEntity.CutterNumber != null && Equals(existingEntity.CutterNumber, groupDto.CutterNumber) &&
                existingEntity.ClassificationNumber != null && Equals(existingEntity.ClassificationNumber, groupDto.ClassificationNumber) &&
                existingEntity.Category.EnglishName.Equals(nameof(LibraryItemCategory.BookSeries))
            );
            if (!isGroupable)
            {
                // Msg: Item {0} cannot group with group {1} as it does not match cutter number, classification number and author with group detail
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0030);
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0030,
                    StringUtils.Format(errMsg, isEng ? $"'{existingEntity.Title}'" : $"'{groupDto.Title}'"));
            }
            else
            {
                // Compare title
                var titleStatus = StringUtils.CompareFieldStatus(
                    StringUtils.RemoveSpecialCharacter(existingEntity.Title),
                    StringUtils.RemoveSpecialCharacter(groupDto.Title));
                
                if(titleStatus == (int)FieldGroupCheckedStatus.GroupFailed)
                {
                    // Msg: Item {0} cannot group with group {1} as it does not match cutter number, classification number and author with group detail
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0030);
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0030,
                        StringUtils.Format(errMsg, isEng ? $"'{existingEntity.Title}'" : $"'{groupDto.Title}'"));
                }
            }
            
            // Assign group id
            existingEntity.GroupId = groupDto.GroupId;
            // Process update
            await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Update group items successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0008));
            }
            
            // Msg: Failed to update group items
            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0006,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0006));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process assign item to group");
        }
    }
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<LibraryItem> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to LibraryItemSpecification
            var itemSpecification = specification as LibraryItemSpecification;
            // Check if specification is null
            if (itemSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total library items
            var totalLibItemWithSpec = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(itemSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalLibItemWithSpec / itemSpecification.PageSize);

            // Set pagination to specification after count total library item
            if (itemSpecification.PageIndex > totalPage
                || itemSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                itemSpecification.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            itemSpecification.ApplyPaging(
                skip: itemSpecification.PageSize * (itemSpecification.PageIndex - 1),
                take: itemSpecification.PageSize);

            // Get all with spec and selector
            var libraryItems = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAndSelectorAsync(itemSpecification, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList()
                });

            if (libraryItems.Any()) // Exist data
            {
                // Convert to dto collection
                var itemDtos = _mapper.Map<List<LibraryItemDto>>(libraryItems);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryItemDto>(itemDtos,
                    itemSpecification.PageIndex, itemSpecification.PageSize, totalPage, totalLibItemWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<LibraryItemDto>>(libraryItems));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke process when get all library item");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, LibraryItemDto dto)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;

                // Ignores authors, library item instances
                if (errors.ContainsKey(StringUtils.ToCamelCase(nameof(LibraryItem.LibraryItemAuthors))))
                {
                    errors.Remove(StringUtils.ToCamelCase(nameof(LibraryItem.LibraryItemAuthors)));
                }
                else if (errors.ContainsKey(nameof(LibraryItem.LibraryItemInstances)))
                {
                    errors.Remove(StringUtils.ToCamelCase(nameof(LibraryItem.LibraryItemInstances)));
                }

                if (errors.Any())
                {
                    throw new UnprocessableEntityException("Invalid validations", errors);
                }
            }

            // Check exist shelf location
            if (dto.ShelfId != null
                && int.TryParse(dto.ShelfId.ToString(), out var validShelfId) &&
                validShelfId > 0) // ShelfId must be numeric
            {
                var checkExistShelfRes = await _libShelfService.AnyAsync(x => x.ShelfId == validShelfId);
                if (checkExistShelfRes.Data is false)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg,
                            isEng ? "shelf location to process update" : "vị trí kệ sách để sửa"));
                }
            }

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == id);
            // Apply including item instances
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemInstances));
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item to process update" : "tài liệu để sửa"));
            }

            // Check exist category
            var toUpdateCategory = (await _cateService.GetWithSpecAsync(
                new BaseSpecification<Category>(c => Equals(c.CategoryId, dto.CategoryId)))).Data as CategoryDto;
            if (toUpdateCategory == null) // Not found
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "category" : "thể loại"));
            }
            else
            {
                // Check whether category is change
                if (!Equals(toUpdateCategory.CategoryId, existingEntity.CategoryId))
                {
                    // Not allow to update to other category when exist at least instance have same prefix as previous category 
                    var isExistWrongPrefix = existingEntity.LibraryItemInstances.Count(li =>
                        !StringUtils.IsValidBarcodeWithPrefix(li.Barcode, toUpdateCategory.Prefix));
                    if (isExistWrongPrefix > 0)
                    {
                        // Error msg: Required all item instance to have the same prefix of new category
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0014,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0014));
                    }
                }
            }

            // Require transitioning to Draft status to modify or soft-delete a book
            if (existingEntity.Status != LibraryItemStatus.Draft)
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
            }

            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();

            // Check duplicate edition number (if change)
            if (!Equals(existingEntity.EditionNumber, dto.EditionNumber))
            {
                // Check whether item within the group
                // Build group spec
                var groupSpec = new BaseSpecification<LibraryItemGroup>(g => g.GroupId == existingEntity.GroupId);
                // Apply including all other library items
                groupSpec.ApplyInclude(q => q
                    .Include(g => g.LibraryItems));
                if ((await _itemGroupService.Value.GetWithSpecAsync(groupSpec)).Data is LibraryItemGroupDto groupDto)
                {
                    // Only process check duplicate edition number when item has already within group
                    var isEditionNumDuplicate = groupDto.LibraryItems
                        // Any other edition number match 
                        .Any(x => x.EditionNumber == dto.EditionNumber);
                    if (isEditionNumDuplicate)
                    {
                        var err = isEng
                            ? "This item has already grouped, item edition number is duplicated with other item"
                            : "Tài liệu này đã được nhóm, số ấn bản bị trùng với tài liệu khác";
                        customErrs.Add(StringUtils.ToCamelCase(nameof(LibraryItem.EditionNumber)), [err]);
                    }
                }
            }

            // Check exist isbn (if change)
            if (!Equals(existingEntity.Isbn, dto.Isbn))
            {
                var isIsbnExist = await _unitOfWork.Repository<LibraryItem, int>()
                    .AnyAsync(be => be.Isbn == dto.Isbn && // Any ISBN found 
                                    be.LibraryItemId != id); // Except request library item
                if (isIsbnExist)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0007);
                    customErrs.Add("isbn", [StringUtils.Format(errMsg, $"'{dto.Isbn}'")]);
                }
            }

            // Check exist cover image
            if (!Equals(existingEntity.CoverImage, dto.CoverImage) // Detect as cover image change 
                && !string.IsNullOrEmpty(dto.CoverImage))
            {
                // Initialize field
                var isImageExist = true;

                // Extract public id from update entity
                var updatePublicId = StringUtils.GetPublicIdFromUrl(dto.CoverImage);
                if (string.IsNullOrEmpty(updatePublicId)) // Provider public id must be existed
                {
                    isImageExist = false;
                }
                else // Exist public id
                {
                    // Check existence on cloud
                    var isImageOnCloud =
                        (await _cloudService.IsExistAsync(updatePublicId, FileType.Image)).Data is true;
                    if (!isImageOnCloud)
                    {
                        isImageExist = false;
                    }
                }

                // Check if existing entity already has image
                if (!string.IsNullOrEmpty(existingEntity.CoverImage))
                {
                    // Extract public id from current entity
                    var currentPublicId = StringUtils.GetPublicIdFromUrl(existingEntity.CoverImage);
                    if (!Equals(currentPublicId, updatePublicId)) // Error invoke when update provider update id 
                    {
                        // Mark as fail to update
                        return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                    }
                }

                if (!isImageExist) // Invoke error image not found
                {
                    // Return as not found image resource
                    return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
                }
            }

            // Check if any errors invoke
            if (customErrs.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrs);
            }

            // Process update entity
            existingEntity.Title = dto.Title;
            existingEntity.SubTitle = dto.SubTitle;
            existingEntity.Responsibility = dto.Responsibility;
            existingEntity.Edition = dto.Edition;
            existingEntity.EditionNumber = dto.EditionNumber;
            existingEntity.Language = dto.Language;
            existingEntity.OriginLanguage = dto.OriginLanguage;
            existingEntity.Summary = dto.Summary;
            existingEntity.CoverImage = dto.CoverImage;
            existingEntity.PublicationYear = dto.PublicationYear;
            existingEntity.Publisher = dto.Publisher;
            existingEntity.PublicationPlace = dto.PublicationPlace;
            existingEntity.ClassificationNumber = dto.ClassificationNumber;
            existingEntity.CutterNumber = dto.CutterNumber;
            existingEntity.Isbn = dto.Isbn != null ? ISBN.CleanIsbn(dto.Isbn) : dto.Isbn;
            existingEntity.Ean = dto.Ean;
            existingEntity.EstimatedPrice = dto.EstimatedPrice;
            existingEntity.PageCount = dto.PageCount;
            existingEntity.PhysicalDetails = dto.PhysicalDetails;
            existingEntity.Dimensions = dto.Dimensions;
            existingEntity.AccompanyingMaterial = dto.AccompanyingMaterial;
            existingEntity.Genres = dto.Genres;
            existingEntity.GeneralNote = dto.GeneralNote;
            existingEntity.BibliographicalNote = dto.BibliographicalNote;
            existingEntity.TopicalTerms = dto.TopicalTerms;
            existingEntity.AdditionalAuthors = dto.AdditionalAuthors;
            existingEntity.CategoryId = dto.CategoryId;
            existingEntity.ShelfId = dto.ShelfId;

            // Progress update when all require passed
            await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(existingEntity);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process update library item");
        }
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        try
        {
            // Determine current lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build a base specification to filter by LibraryItemId
            var baseSpec = new BaseSpecification<LibraryItem>(a => a.LibraryItemId == id);
            // Apply including authors
            baseSpec.ApplyInclude(q => q
                .Include(be => be.LibraryItemAuthors));

            // Retrieve library item with specification
            var itemEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (itemEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item to process delete" : "tài liệu để xóa"));
            }

            // Check whether library item in the trash bin or not in draft status
            if (!itemEntity.IsDeleted)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Try to delete all library item authors (if any)
            if (itemEntity.LibraryItemAuthors.Any())
            {
                // Process delete range without save changes
                await _itemAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
                    // Select all existing libraryItemAuthorId
                    itemEntity.LibraryItemAuthors.Select(ba => ba.LibraryItemAuthorId).ToArray());
            }

            // Perform delete library item, and delete cascade with LibraryItemInventory
            await _unitOfWork.Repository<LibraryItem, int>().DeleteAsync(id);

            // Save to DB
            if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress delete data");
        }
    }

    public async Task<IServiceResult> GetAllWithoutAdvancedSpecAsync(
        ISpecification<LibraryItem> specification,
        bool tracked = true)
    {
        return await base.GetAllWithSpecAsync(specification, tracked);
    }
    
    public async Task<IServiceResult> GetDetailAsync(int id, string? email = null)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItem>(b => b.LibraryItemId == id);
            var itemEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances.Select(li => new LibraryItemInstance()
                    {
                        LibraryItemInstanceId = li.LibraryItemInstanceId,
                        LibraryItemId = li.LibraryItemId,
                        Barcode = li.Barcode,
                        Status = li.Status,
                        CreatedAt = li.CreatedAt,
                        UpdatedAt = li.UpdatedAt,
                        CreatedBy = li.CreatedBy,
                        UpdatedBy = li.UpdatedBy,
                        IsDeleted = li.IsDeleted,
                        IsCirculated = li.IsCirculated,
                        LibraryItemConditionHistories = li.LibraryItemConditionHistories.Select(lih => new LibraryItemConditionHistory()
                        {
                            ConditionHistoryId = lih.ConditionHistoryId,
                            LibraryItemInstanceId = lih.LibraryItemInstanceId,
                            ConditionId = lih.ConditionId,
                            CreatedAt = lih.CreatedAt,
                            UpdatedAt = lih.UpdatedAt,
                            CreatedBy = lih.CreatedBy,
                            UpdatedBy = lih.UpdatedBy,
                            Condition = lih.Condition,
                        }).ToList(),
                    }).ToList(),
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList(),
                    LibraryItemResources = be.LibraryItemResources.Select(lir => new LibraryItemResource()
                    {
                        LibraryItemResourceId = lir.LibraryItemResourceId,
                        LibraryItemId = lir.LibraryItemId,
                        ResourceId = lir.ResourceId,
                        LibraryResource = lir.LibraryResource
                    }).ToList()
                });

            if (itemEntity != null)
            {
                // Define a local mapping configuration
                var localConfig = new TypeAdapterConfig();
                localConfig.NewConfig<LibraryItem, LibraryItemDto>()
                    .Map(dest => dest.LibraryItemResources, 
                        src => src.LibraryItemResources.Adapt<List<LibraryItemResourceDto>>(localConfig)) // Explicitly map LibraryItemResources
                    .AfterMapping((src, dest) => 
                    {
                        foreach (var resource in dest.LibraryItemResources)
                        {
                            if (!string.IsNullOrEmpty(email))
                            {
                                // Ignore ResourceUrl
                                resource.LibraryResource.ResourceUrl = null!; 
                            }
                        }
                    });
                
                // Map to dto
                var dto = itemEntity.Adapt<LibraryItemDto>(localConfig);
                
                // Initialize collection of digital borrow
                var digitalBorrows = new List<DigitalBorrowDto>();
                // Extract all item's resource ids
                var resourceIds = dto.LibraryItemResources.Select(lir => lir.ResourceId).ToArray();
                // Try to retrieve user and their digital borrows
                var userSpec = new BaseSpecification<User>(u => 
                    u.Email == email &&
                    u.DigitalBorrows.Any(db => resourceIds.Contains(db.ResourceId)));
                // Apply include
                userSpec.ApplyInclude(q => q
                    .Include(u => u.DigitalBorrows)
                        .ThenInclude(d => d.DigitalBorrowExtensionHistories)
                );
                if ((await _userService.Value.GetWithSpecAsync(userSpec)).Data is UserDto userDto)
                {
                    // Order by digital borrow id 
                    userDto.DigitalBorrows = userDto.DigitalBorrows.OrderBy(db => db.DigitalBorrowId).ToList();
                    
                    // Group by ResourceId and select the newest (last item in the ordered list)
                    var newestBorrows = userDto.DigitalBorrows
                        .GroupBy(db => db.ResourceId)
                        .Select(g => g.Last())
                        .ToList();
                    
                    // Remove user information in each digital borrow
                    newestBorrows.ForEach(db => db.User = null!);
                    
                    // Add range new digital borrows
                    digitalBorrows.AddRange(newestBorrows);
                }
                
                // Initialize instance units
                var totalInShelfUnits = 0;
                var totalOutOfShelfUnits = 0;
                // Initialize count spec
                var countInSpec = new BaseSpecification<LibraryItemInstance>();
                // Count total in-shelf units
                countInSpec.AddFilter(li =>
                    li.Status == nameof(LibraryItemInstanceStatus.InShelf) && // In-shelf status
                    li.LibraryItemId == itemEntity.LibraryItemId); // Exclude update instance
                if ((await _itemInstanceService.Value.CountAsync(countInSpec)).Data is int countInRes)
                {
                    totalInShelfUnits = countInRes;
                }

                // Count total out-of-shelf units
                var countOutSpec = new BaseSpecification<LibraryItemInstance>();
                countOutSpec.AddFilter(li =>
                    li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf) && // In-shelf status
                    li.LibraryItemId == itemEntity.LibraryItemId); // Exclude update instance
                if ((await _itemInstanceService.Value.CountAsync(countOutSpec)).Data is int countOutRes)
                {
                    totalOutOfShelfUnits = countOutRes; 
                }
                
                // Retrieve AI training session
                var trainingSession = (await _aiTrainingSessionSvc.Value.GetByLibraryItemIdAsync(itemEntity.LibraryItemId)
                    ).Data as AITrainingSessionDto;
                
                // Convert to library item detail dto
                var itemDetailDto = dto.ToLibraryItemDetailDto(
                    digitalBorrows: digitalBorrows,
                    aiTrainingSession: trainingSession,
                    totalInstancesInShelf: totalInShelfUnits,
                    totalInstanceOutOfShelf: totalOutOfShelfUnits);
                
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), itemDetailDto);
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get library item by id");
        }
    }

    public async Task<IServiceResult> GetByBarcodeAsync(string barcode)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItem>(l => 
                l.LibraryItemInstances.Any(i => Equals(i.Barcode, barcode)));
            var itemEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList(),
                    LibraryItemResources = be.LibraryItemResources.Select(lir => new LibraryItemResource()
                    {
                        LibraryItemResourceId = lir.LibraryItemResourceId,
                        LibraryItemId = lir.LibraryItemId,
                        ResourceId = lir.ResourceId,
                        LibraryResource = lir.LibraryResource
                    }).ToList(),
                });

            if (itemEntity != null)
            {
                // Map to dto
                var dto = _mapper.Map<LibraryItemDto>(itemEntity);

                // Convert to library item detail dto
                var itemDetailDto = dto.ToLibraryItemDetailDto();

                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), itemDetailDto);
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get library item by barcode");
        }
    }
    
    public async Task<IServiceResult> GetByIsbnAsync(string isbn)
    {
        try
        {
            // Build specification
            var cleanedIsbn = ISBN.CleanIsbn(isbn);
            var baseSpec = new BaseSpecification<LibraryItem>(l => Equals(l.Isbn, cleanedIsbn));
            var itemEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList(),
                    LibraryItemResources = be.LibraryItemResources.Select(lir => new LibraryItemResource()
                    {
                        LibraryItemResourceId = lir.LibraryItemResourceId,
                        LibraryItemId = lir.LibraryItemId,
                        ResourceId = lir.ResourceId,
                        LibraryResource = lir.LibraryResource
                    }).ToList(),
                });

            if (itemEntity != null)
            {
                // Map to dto
                var dto = _mapper.Map<LibraryItemDto>(itemEntity);

                // Convert to library item detail dto
                var itemDetailDto = dto.ToLibraryItemDetailDto();

                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), itemDetailDto);
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get library item by barcode");
        }
    }
    
    public async Task<IServiceResult> GetEnumValueAsync()
    {
        try
        {
            // Book resource types
            var resourceTypes = new List<string>()
            {
                nameof(LibraryResourceType.Ebook),
                nameof(LibraryResourceType.AudioBook)
            };

            // File formats
            var fileFormats = new List<string>()
            {
                nameof(FileType.Image),
                nameof(FileType.Video)
            };

            // Resource provider
            var resourceProviders = new List<string>()
            {
                nameof(ResourceProvider.Cloudinary)
            };

            // library item instance statuses
            var itemInstanceStatus = new List<string>()
            {
                nameof(LibraryItemInstanceStatus.InShelf),
                nameof(LibraryItemInstanceStatus.OutOfShelf),
                nameof(LibraryItemInstanceStatus.Borrowed),
                nameof(LibraryItemInstanceStatus.Reserved),
                nameof(LibraryItemInstanceStatus.Lost),
            };

            // Copy condition statuses
            var conditionStatuses = new List<string>
            {
                nameof(LibraryItemConditionStatus.Good),
                nameof(LibraryItemConditionStatus.Worn),
                nameof(LibraryItemConditionStatus.Damaged),
                nameof(LibraryItemConditionStatus.Lost)
            };

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                new
                {
                    ResourceTypes = resourceTypes,
                    FileFormats = fileFormats,
                    ResourceProviders = resourceProviders,
                    ItemInstanceStatuses = itemInstanceStatus,
                    ConditionStatuses = conditionStatuses
                });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get library item enum value");
        }
    }

    public async Task<IServiceResult> GetRecentReadByIdsAsync(int[] ids, int pageIndex, int pageSize)
    {
        try
        {
            // Try to get items that match request ids
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => ids.Contains(li.LibraryItemId));
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Apply include
            baseSpec.ApplyInclude(q => q
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

            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);
            
            // Set pagination to specification after count total 
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            baseSpec.ApplyPaging(
                skip: (pageIndex - 1) * pageSize,
                take: pageSize);
            
            // Add order by
            baseSpec.AddOrderBy(li => li.LibraryItemId);
            
            var entities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Map to home page item dto
                var homePageItemDtos = 
                    _mapper.Map<List<LibraryItemDto>>(entities).Select(x => x.ToHomePageItemDto());
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<HomePageItemDto>(homePageItemDtos,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<HomePageItemDto>(new List<HomePageItemDto>(), 0,0,0,0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get multiple data");
        }
    }

    public async Task<IServiceResult> GetNewArrivalsAsync(int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.Status == LibraryItemStatus.Published);
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Add order descending by create datetime 
            baseSpec.AddOrderByDescending(li => li.CreatedAt);
            
            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);
            
            // Set pagination to specification after count total 
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            baseSpec.ApplyPaging(
                skip: (pageIndex - 1) * pageSize,
                take: pageSize);
            
            var entities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Map to home page item dto
                var homePageItemDtos = 
                    _mapper.Map<List<LibraryItemDto>>(entities).Select(x => x.ToHomePageItemDto());
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<HomePageItemDto>(homePageItemDtos,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<HomePageItemDto>(new List<HomePageItemDto>(), 0,0,0,0));
            
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get new arrival library items");
        }
    }
    
    public async Task<IServiceResult> GetTrendingAsync(int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.Status == LibraryItemStatus.Published);
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Apply include
            baseSpec.ApplyInclude(q => q
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
            
            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);
            
            // Set pagination to specification after count total 
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            baseSpec.ApplyPaging(
                skip: (pageIndex - 1) * pageSize,
                take: pageSize);
            
            // Add order by
            baseSpec.AddOrderByDescending(li => li.LibraryItemReviews.Average(x => x.RatingValue));
            
            var entities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Map to home page item dto
                var homePageItemDtos = 
                    _mapper.Map<List<LibraryItemDto>>(entities).Select(x => x.ToHomePageItemDto());
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<HomePageItemDto>(homePageItemDtos,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<HomePageItemDto>(new List<HomePageItemDto>(), 0,0,0,0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get trending item");
        }
    }

    public async Task<IServiceResult> GetByCategoryAsync(int categoryId, int pageIndex, int pageSize)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist category
            var isExistCategory = (await _cateService.AnyAsync(c => c.CategoryId == categoryId)).Data is true;
            if (!isExistCategory)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "category" : "phân loại"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => 
                li.CategoryId == categoryId && li.Status == LibraryItemStatus.Published);
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Apply include
            baseSpec.ApplyInclude(q => q
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
            
            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);
            
            // Set pagination to specification after count total 
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            baseSpec.ApplyPaging(
                skip: (pageIndex - 1) * pageSize,
                take: pageSize);
            
            // Add order by
            baseSpec.AddOrderByDescending(li => li.LibraryItemReviews.Average(x => x.RatingValue));
            
            var entities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Map to home page item dto
                var homePageItemDtos = 
                    _mapper.Map<List<LibraryItemDto>>(entities).Select(x => x.ToHomePageItemDto());
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<HomePageItemDto>(homePageItemDtos,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<HomePageItemDto>(new List<HomePageItemDto>(), 0,0,0,0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get trending item");
        }
    }

    public async Task<IServiceResult> GetItemsInGroupAsync(int id, int pageIndex, int pageSize)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItem>(b => b.LibraryItemId == id);
            // Retrieve library item and its group items
            var libraryItemEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, li => new LibraryItem()
                {
                    LibraryItemId = li.LibraryItemId,
                    LibraryItemGroup = li.LibraryItemGroup != null && li.LibraryItemGroup.LibraryItems.Any()
                        ? new LibraryItemGroup()
                        {
                            LibraryItems = li.LibraryItemGroup.LibraryItems
                                .Where(li2 => li2.LibraryItemId != id)
                                .Select(li2 => new LibraryItem()
                            {
                                LibraryItemId = li2.LibraryItemId,
                                Title = li2.Title,
                                SubTitle = li2.SubTitle,
                                Responsibility = li2.Responsibility,
                                Edition = li2.Edition,
                                EditionNumber = li2.EditionNumber,
                                Language = li2.Language,
                                OriginLanguage = li2.OriginLanguage,
                                Summary = li2.Summary,
                                CoverImage = li2.CoverImage,
                                PublicationYear = li2.PublicationYear,
                                Publisher = li2.Publisher,
                                PublicationPlace = li2.PublicationPlace,
                                ClassificationNumber = li2.ClassificationNumber,
                                CutterNumber = li2.CutterNumber,
                                Isbn = li2.Isbn,
                                Ean = li2.Ean,
                                EstimatedPrice = li2.EstimatedPrice,
                                PageCount = li2.PageCount,
                                PhysicalDetails = li2.PhysicalDetails,
                                Dimensions = li2.Dimensions,
                                AccompanyingMaterial = li2.AccompanyingMaterial,
                                Genres = li2.Genres,
                                GeneralNote = li2.GeneralNote,
                                BibliographicalNote = li2.BibliographicalNote,
                                TopicalTerms = li2.TopicalTerms,
                                AdditionalAuthors = li2.AdditionalAuthors,
                                CategoryId = li2.CategoryId,
                                ShelfId = li2.ShelfId,
                                GroupId = li2.GroupId,
                                Status = li2.Status,
                                IsDeleted = li2.IsDeleted,
                                IsTrained = li2.IsTrained,
                                CanBorrow = li2.CanBorrow,
                                TrainedAt = li2.TrainedAt,
                                CreatedAt = li2.CreatedAt,
                                UpdatedAt = li2.UpdatedAt,
                                UpdatedBy = li2.UpdatedBy,
                                CreatedBy = li2.CreatedBy,
                                // References
                                Category = li2.Category,
                                LibraryItemReviews = li2.LibraryItemReviews,
                                LibraryItemAuthors = li2.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                                {
                                    LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                    LibraryItemId = ba.LibraryItemId,
                                    AuthorId = ba.AuthorId,
                                    Author = ba.Author
                                }).ToList(),
                            }).ToList()
                        } 
                        : null,
                });

            if (libraryItemEntity != null &&
                libraryItemEntity.LibraryItemGroup != null &&
                libraryItemEntity.LibraryItemGroup.LibraryItems.Any())
            {
                // Map to dto
                var dto = _mapper.Map<LibraryItemDto>(libraryItemEntity);
                // Retrieve all other items in group and map to detail 
                var otherItemsInGroup = dto.LibraryItemGroup?.LibraryItems
                    .Select(li => li.ToLibraryItemGroupedDetailDto()).ToList();

                if (otherItemsInGroup != null && otherItemsInGroup.Any())
                {
                    // Pagination
                    var totalActualItem = otherItemsInGroup.Count;
                    var totalPage = (int) Math.Ceiling((double) totalActualItem / pageSize);
                    
                    // Validation pagination fields
                    if (pageIndex < 1 || pageIndex > totalPage) pageIndex = 1;
                    
                    // Apply pagination
                    otherItemsInGroup = otherItemsInGroup.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                
                    // Pagination result 
                    var paginationResultDto = new PaginatedResultDto<LibraryItemDetailDto>(otherItemsInGroup,
                        pageIndex, pageSize, totalPage, totalActualItem);
                    
                    return new ServiceResult(ResultCodeConst.SYS_Success0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
                }
            }

            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<LibraryItemDetailDto>(
                    new List<LibraryItemDetailDto>(), 0,0,0,0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get items in group");
        }
    }

    public async Task<IServiceResult> GetReviewsAsync(int id, int pageIndex, int pageSize)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemReviews)
                    .ThenInclude(lir => lir.User)
            );
            // Retrieve all reviews
            var libraryItem = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAsync(baseSpec);
            if (libraryItem != null)
            {
                // Extract all reviews, convert to dto
                var libraryItemReviews = _mapper.Map<List<LibraryItemReviewDto>>(libraryItem.LibraryItemReviews.ToList());
                
                // Apply pagination
                var totalActualItem = libraryItemReviews.Count;
                var totalPage = (int) Math.Ceiling((double) totalActualItem / pageSize);
                
                // Validate pagination fields
                if (pageIndex < 1 || pageIndex > totalPage) pageIndex = 1;
                
                // Apply pagination
                libraryItemReviews = libraryItemReviews
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryItemReviewDto>(libraryItemReviews,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Response with review dtos
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new PaginatedResultDto<LibraryItemReviewDto>(
                    new List<LibraryItemReviewDto>(), 0,0,0,0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get item reviews");
        }
    }

    public async Task<IServiceResult> GetRelatedItemsAsync(int id, int pageIndex, int pageSize)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check existing item 
            var rootItemEntity = await _unitOfWork.Repository<LibraryItem, int>().GetByIdAsync(id);
            if (rootItemEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"));
            }
            
            // Extract genres of the root item
            var rootGenres = rootItemEntity.Genres?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .ToList();
            var rootTopicalTerms = rootItemEntity.TopicalTerms?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .First();
            if (rootGenres == null || !rootGenres.Any())
            {
                // Response empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new PaginatedResultDto<LibraryItemDetailDto>(
                        new List<LibraryItemDetailDto>(), 0,0,0,0));
            }
            
            // Build specification to find all items has at least one genre
            var baseSpec = new BaseSpecification<LibraryItem>(li =>
                li.Status == LibraryItemStatus.Published && // Must be published
                li.LibraryItemId != id && // Exclude root item
                (
                    (
                        !string.IsNullOrEmpty(li.TopicalTerms) && !string.IsNullOrEmpty(rootTopicalTerms) &&
                        li.TopicalTerms.Contains(rootTopicalTerms)
                    ) ||
                    ( 
                        !string.IsNullOrEmpty(li.Genres) &&
                        rootGenres.Any(rootGenre => li.Genres.Contains(rootGenre))
                    )
                )); // Exist at least genre with root item
            // Enable split query for optimization
            baseSpec.EnableSplitQuery();
            // Apply includes if necessary
            baseSpec.ApplyInclude(q => q
                .Include(li => li.Category)
                .Include(li => li.LibraryItemAuthors)
                    .ThenInclude(lia => lia.Author)
                .Include(li => li.LibraryItemReviews)
            );
            
            // Retrieve all potential related items (match at least one genre)
            var relatedItems = await _unitOfWork.Repository<LibraryItem, int>().GetAllWithSpecAsync(baseSpec);
            // Convert to list 
            var relatedItemList = relatedItems.ToList();
            if (!relatedItemList.Any())
            {
                // Response empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new PaginatedResultDto<LibraryItemDetailDto>(
                        new List<LibraryItemDetailDto>(), 0,0,0,0));
            }
            
            // Increase relevance score by count matching genre 
            var itemsWithMatchCount = relatedItemList.Select(item =>
            {
                var itemGenres = item.Genres?
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .ToList() ?? new List<string>();
            
                // Count the number of matching genres
                var matchCount = itemGenres.Intersect(rootGenres).Count();
            
                return new
                {
                    Item = item,
                    MatchCount = matchCount
                };
            })
            .OrderByDescending(x => x.MatchCount) // Order by most matching genres
            .Select(x => x.Item) // Extract the items
            .ToList();
            
            // Convert to DTOs and details
            var dtos = _mapper.Map<List<LibraryItemDto>>(itemsWithMatchCount);
            var details = dtos.Select(li => li.ToLibraryItemGroupedDetailDto());

            // Pagination
            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);
            
            // Set pagination to specification after count total 
            if (pageIndex > totalPage || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            details = details.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            
            // Pagination result 
            var paginationResultDto = new PaginatedResultDto<LibraryItemDetailDto>(details,
                pageIndex, pageSize, totalPage, totalActualItem);
            
            // Return the result
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get related items");
        }
    }

    public async Task<IServiceResult> GetFirstAuthorAsync(int id)
    {
        try
        {
            return await _itemAuthorService.Value.GetFirstByLibraryItemIdAsync(libraryItemId: id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get first author for specific item");
        }
    }
    
    public async Task<IServiceResult> GetByInstanceBarcodeAsync(string barcode)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemInstances
                .Any(inst => Equals(inst.Barcode, barcode)));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemInstances)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            // Read success
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<LibraryItemDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get item by instance barcode");
        }
    }

    public async Task<IServiceResult> GetItemClassificationNumAsync(int id)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == id);
            // Get with spec and selector
            var itemClassificationNum = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, selector: s => s.ClassificationNumber);
            
            // Mark as call success
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: itemClassificationNum);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get item classification number by id");
        }
    }
    
    public async Task<IServiceResult> GetItemClassificationNumAsync(int[] ids)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => ids.Contains(li.LibraryItemId));
            // Get with spec and selector
            var itemClassificationNums = (await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, selector: s => s.ClassificationNumber)).ToList();

            // Mark as call success
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: itemClassificationNums.Distinct().ToList());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get item classification number by id");
        }
    }
    
    public async Task<IServiceResult> GetItemClassificationNumForVectorAsync(int[] ids)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => ids.Contains(li.LibraryItemId));
            // Get with spec and selector
            var itemClassificationNums = (await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, selector: s => new GetVectorClassificationNumResult()
                {
                    LibraryItemId = s.LibraryItemId,
                    ClassificationNumber = s.ClassificationNumber
                })).ToList();

            // Mark as call success
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: itemClassificationNums.ToList());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get item classification number by id");
        }
    }

    public async Task<IServiceResult> GetAllForRecommendationAsync()
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => 
                li.Status == LibraryItemStatus.Published); // In published status (able to circulation)
            // Retrieve all with spec
            var entities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList()
                }, tracked: false);
            if (entities.Any())
            {
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: _mapper.Map<List<LibraryItemDto>>(entities));
            }
            
            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                data: new List<LibraryItemDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all items for recommendation");
        }
    }

    public async Task<IServiceResult> GetHighBorrowOrReserveRateItemsAsync(int? pageIndex, int? pageSize)
    {
        try
        {
            // Pagination
            int validPageIndex = pageIndex ?? 1;
            int validPageSize = pageSize ?? _appSettings.PageSize;
            
            // Initialize base spec to retrieve building filter when operator is 'includes'
            List<Expression<Func<LibraryItem, bool>>> includeExpressions = new();
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(li => li.Status == LibraryItemStatus.Published);
            
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Add filter
            includeExpressions.Add(li => li.LibraryItemInstances.Count(l => l.BorrowRecordDetails.Count > 0) > 0);
            includeExpressions.Add(li => li.LibraryItemInstances.Count(l => l.ReservationQueues.Count > 0) > 0);
            includeExpressions.Add(li => li.BorrowRequestDetails.Count > 0);
            if (includeExpressions.Any())
            {
                var resultExpression = includeExpressions.Skip(1).Aggregate(includeExpressions.FirstOrDefault(),
                    (exp1, exp2) =>
                    {
                        if (exp1 != null)
                        {
                            // Try to combined body of different expression with 'OR' operator
                            var body = Expression.OrElse(exp1.Body, Expression.Invoke(exp2, exp1.Parameters));
                                                    
                            // Return combined body
                            return Expression.Lambda<Func<LibraryItem, bool>>(body, exp1.Parameters);
                        }
                
                        return _ => false;
                    });
                                        
                // Apply filter with 'includes'
                if(resultExpression != null) baseSpec.AddFilter(resultExpression);
            }
            
            // Apply order
            baseSpec.AddOrderByDescending(li =>
                // 1) number of direct BorrowRequestDetails
                li.BorrowRequestDetails.Count()
                // 2) total BorrowRecordDetails across all instances
                + li.LibraryItemInstances
                    .SelectMany(inst => inst.BorrowRecordDetails)
                    .Count()
                // 3) total ReservationQueues across all instances
                + li.LibraryItemInstances
                    .SelectMany(inst => inst.ReservationQueues)
                    .Count()
            );
            
            // Count total actual item
            var totalActualItem = await _unitOfWork.Repository<LibraryItem, int>().CountAsync();
            
            // Initialize response item list
            var responseItems = new List<LibraryItemDto>();
            // Check exist any items match 
            var existItemMatch = await _unitOfWork.Repository<LibraryItem, int>().AnyAsync(baseSpec);
            if (existItemMatch)
            {
                // Retrieve all items match filter
                var itemsMatch = (await _unitOfWork.Repository<LibraryItem, int>().GetAllWithSpecAsync(baseSpec)).ToList();
                // Map to dto
                responseItems = _mapper.Map<List<LibraryItemDto>>(itemsMatch);
            }
            
            // Check whether response items not reach page size threshold
            if (responseItems.Count < validPageSize)
            {
                // Extract all exist item in response items
                var existingItemIds = responseItems.Select(i => i.LibraryItemId).ToList();
                // Build spec
                var additionSpec = new BaseSpecification<LibraryItem>(li => li.Status == LibraryItemStatus.Published);
                // Add filter
                additionSpec.AddFilter(li => !existingItemIds.Contains(li.LibraryItemId));
                // Add order by created date
                additionSpec.AddOrderByDescending(li => li.CreatedAt);
                
                // Retrieve all items with spec
                var itemsMatch = (await _unitOfWork.Repository<LibraryItem, int>().GetAllWithSpecAsync(additionSpec)).ToList();
                // Take N items to reach page size threshold
                var totalToFull = validPageSize - responseItems.Count;
                // Process take N items
                itemsMatch = itemsMatch.Take(totalToFull).ToList();
                // Append to response items list
                responseItems.AddRange(_mapper.Map<List<LibraryItemDto>>(itemsMatch));
            }

            if (responseItems.Any())
            {
                // Process add pagination result
                // Count total library items
                var totalItem = responseItems.Count;
                // Count total page
                var totalPage = (int)Math.Ceiling((double)totalActualItem / validPageSize);
                // Set pagination to specification after count total library item
                if (validPageIndex > totalPage || validPageIndex < 1) // Exceed total page or page index smaller than 1
                {
                    validPageIndex = 1; // Set default to first page
                }
                // Apply pagination
                responseItems = responseItems.Skip((validPageIndex - 1) * validPageSize).Take(validPageSize).ToList();
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryItemDto>(responseItems,
                    validPageIndex, validPageSize, totalPage, totalActualItem);
                
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                data: new List<LibraryItemDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get high borrow or reserve rate items");
        }
    }

    public async Task<IServiceResult> CheckUnavailableForBorrowRequestAsync(string email, int[] ids)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist user with card
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            // Apply include
            userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
            // Retrieve user with spec
            var userDto = (await _userService.Value.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access");

            // Check whether user card is exist
            if (userDto.LibraryCardId != null && Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId))
            {
                // Try to validate card
                var checkCardRes = await _cardService.Value.CheckCardValidityAsync(validCardId);
                if (checkCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001)
                {
                    // Custom message for not found
                    if (checkCardRes.ResultCode == ResultCodeConst.SYS_Warning0002)
                    {
                        var customMsg = isEng
                            ? "Please register library card to create borrow request"
                            : "Vui lòng đăng ký kẻ thư viện để có thể mượn sách";
                        return new ServiceResult(ResultCodeConst.SYS_Warning0002, customMsg);
                    }

                    // Return handled message
                    return checkCardRes;
                }
            }
            else
            {
                // Msg: You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Build spec
            var baseSpec =
                new BaseSpecification<LibraryItem>(li =>
                    ids.Contains(li.LibraryItemId)); // include id in requested list
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItemInventory)
                .Include(li => li.Category)
                .Include(li => li.Shelf)
                .Include(li => li.LibraryItemAuthors)
                .ThenInclude(lia => lia.Author)
                .Include(li => li.LibraryItemReviews)
            );

            // Retrieve all data with spec
            var entities = (await _unitOfWork.Repository<LibraryItem, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            if (entities.Any())
            {
                // Initialize response collections
                var alreadyBorrowedItems = new List<HomePageItemDto>();
                var alreadyRequestedItems = new List<HomePageItemDto>();
                var alreadyReservedItems = new List<HomePageItemDto>();
                var allowToReserveItems = new List<HomePageItemDto>();
                var allowToBorrowItems = new List<HomePageItemDto>();

                // Map to home page item dto
                var homePageItemDtos =
                    _mapper.Map<List<LibraryItemDto>>(entities).Select(x => x.ToHomePageItemDto()).ToList();

                // Iterate all items to check for borrowed item
                foreach (var dto in homePageItemDtos)
                {
                    // Check whether item has been reserved
                    var hasReservedConstraint = (await _reservationQueueService.Value.AnyAsync(r =>
                        r.LibraryItemId == dto.LibraryItemId &&
                        r.LibraryCardId == userDto.LibraryCardId &&
                        r.QueueStatus != ReservationQueueStatus.Expired &&
                        r.QueueStatus != ReservationQueueStatus.Cancelled &&
                        r.QueueStatus != ReservationQueueStatus.Collected)).Data is true;
                    // Has constraint
                    if (hasReservedConstraint)
                    {
                        // Add to already reserved list
                        alreadyReservedItems.Add(dto);

                        // Skip to next item
                        continue;
                    }

                    // Check whether item has been borrowed
                    var hasBorrowRecordConstraint = (await _borrowRecordService.Value.AnyAsync(bRec =>
                                bRec.LibraryCardId == userDto.LibraryCardId &&
                                bRec.BorrowRecordDetails.Any(brd =>
                                    brd.LibraryItemInstance.LibraryItemId == dto.LibraryItemId && // Exist in any borrow record details
                                    brd.Status != BorrowRecordStatus.Returned)) // Exclude elements with returned status
                        ).Data is true; // Convert object to boolean 
                    // Has constraint
                    if (hasBorrowRecordConstraint)
                    {
                        // Add to already borrowed list
                        alreadyBorrowedItems.Add(dto);

                        // Skip to next item
                        continue;
                    }

                    // Check whether item has been requested to borrow
                    var hasBorrowReqConstraint = (await _borrowRequestService.Value.AnyAsync(
                        br => br.LibraryCardId == userDto.LibraryCardId &&
                              br.Status == BorrowRequestStatus.Created &&
                              br.BorrowRequestDetails.Any(brd => brd.LibraryItemId == dto.LibraryItemId))).Data is true;
                    // Has constraint
                    if (hasBorrowReqConstraint)
                    {
                        // Add to already requested list
                        alreadyRequestedItems.Add(dto);
                    }
                }

                // Extract all already borrowed item ids
                var alreadyBorrowedItemIds = alreadyBorrowedItems.Select(x => x.LibraryItemId).ToList();
                var alreadyRequestedItemIds = alreadyRequestedItems.Select(x => x.LibraryItemId).ToList();
                var alreadyReservedItemIds = alreadyReservedItems.Select(x => x.LibraryItemId).ToList();
                // Filter all items which available units is zero to process create reservation
                var unavailableFilteredItems = homePageItemDtos.Where(li =>
                    li.LibraryItemInventory != null &&
                    !alreadyBorrowedItemIds.Contains(li.LibraryItemId) &&
                    !alreadyRequestedItemIds.Contains(li.LibraryItemId) &&
                    !alreadyReservedItemIds.Contains(li.LibraryItemId));

                // Iterate each library item to check for allowing to reserve
                foreach (var dto in unavailableFilteredItems)
                {
                    // Check exist any pending reservation by item instance id
                    var isAllowToReserve = (await _reservationQueueService.Value.CheckAllowToReserveByItemIdAsync(
                        itemId: dto.LibraryItemId,
                        email: email)).Data is true;
                    if (isAllowToReserve) // Is allow to reserve
                    {
                        allowToReserveItems.Add(dto);
                    }
                    else // Is allow to borrow
                    {
                        allowToBorrowItems.Add(dto);
                    }
                }

                // Validate amount of allowing to borrow items
                var validateAmountRes = await _borrowRequestService.Value.ValidateBorrowAmountAsync(
                    totalItem: allowToBorrowItems.Count,
                    libraryCardId: validCardId);
                // Check whether exceeding than threshold
                if (validateAmountRes != null) return validateAmountRes;

                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), new
                    {
                        AlreadyRequestedItems = alreadyRequestedItems,
                        AlreadyBorrowedItems = alreadyBorrowedItems,
                        AlreadyReservedItems = alreadyReservedItems,
                        AllowToReserveItems = allowToReserveItems,
                        AllowToBorrowItems = allowToBorrowItems
                    });
            }

            // Response empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<HomePageItemDto>());
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check unavailable items");
        }
    }
    
    public async Task<IServiceResult> UpdateStatusAsync(int id)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                // Include edition inventory
                .Include(x => x.LibraryItemInventory)
                // Include item category
                .Include(x => x.Category)
                // Include library item authors
                .Include(x => x.LibraryItemAuthors)
                // Include author
                .ThenInclude(bea => bea.Author)
                // Include library item instances
                .Include(x => x.LibraryItemInstances)
                // Include library shelf
                .Include(x => x.Shelf)!
            );

            // Retrieve library item with specific ID 
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null || existingEntity.IsDeleted) // Check whether book exist or marking as deleted
            {
                var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMSg, isEng ? "library item" : "tài liệu"));
            }

            // Check current library item status
            if (existingEntity.Status == LibraryItemStatus.Draft) // Draft -> Published
            {
                // Initialize err msg
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0012);
                // Validate edition information before published
                // Check for shelf location
                if (existingEntity.ShelfId == null || existingEntity.ShelfId == 0)
                {
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0012,
                        StringUtils.Format(errMsg, isEng
                            ? "Shelf location not found"
                            : "Không tìm thấy vị trí kệ cho sách"));
                }

                if (existingEntity.ShelfId > 0)
                {
                    // Check for exist at least one library item copy that mark as in shelf
                    if (existingEntity.LibraryItemInstances.All(x =>
                            x.Status != nameof(LibraryItemInstanceStatus.InShelf)))
                    {
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0012,
                            StringUtils.Format(errMsg, isEng
                                ? "Required at least one item in shelf"
                                : "Cần ít nhất một bản in có sẵn trên kệ"));
                    }
                }

                // Process change status
                existingEntity.Status = LibraryItemStatus.Published;

                // Process update change to DB
                if (await _unitOfWork.SaveChangesAsync() > 0) // Success
                {
                    // Initialize bool field
                    var isAddToElastic = false;
                    // Synchronize data to ElasticSearch
                    if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.LibraryItemIndex))
                    {
                        // Convert to LibraryItemDto
                        var dto = _mapper.Map<LibraryItemDto>(existingEntity);

                        // Try to add (if not exist) or update (if already exist) elastic document
                        // Process add both root and nested object
                        isAddToElastic = await _elasticService.Value.AddOrUpdateAsync(
                            document: dto.ToElasticLibraryItem(),
                            documentKeyName: nameof(ElasticLibraryItem
                                .LibraryItemId)); // Custom elastic _id with LibraryItemId value
                    }

                    // Custom message for failing to synchronize data to elastic
                    var msg = !isAddToElastic
                        ? isEng
                            ? ", but fail to add data to Elastic"
                            : ", nhưng cập nhật dữ liệu mới vào Elastic thất bại"
                        : string.Empty;
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
                }
            }
            else if (existingEntity.Status == LibraryItemStatus.Published) // Published -> Draft
            {
                // Initialize err msg
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0013);
                // Check whether book in borrow status (One or more copy now in store in library shelf) 
                if (existingEntity.CanBorrow)
                {
                    // Do not allow to change status
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0013,
                        StringUtils.Format(errMsg,
                            isEng ? "There still exist item in shelf" : "Vẫn còn tài liệu ở trên kệ"));
                }

                // Check inventory total whether to allow change status to Draft
                if (existingEntity.LibraryItemInventory != null &&
                    (existingEntity.LibraryItemInventory.RequestUnits > 0 ||
                     existingEntity.LibraryItemInventory.BorrowedUnits > 0 ||
                     existingEntity.LibraryItemInventory.ReservedUnits > 0))
                {
                    // Cannot change data that is on borrowing or reserved
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0013,
                        StringUtils.Format(errMsg, isEng
                            ? "Item is on borrowing or reserved"
                            : "Tài liệu đang được mượn hoặc được đặt trước"));
                }

                // Process change status
                existingEntity.Status = LibraryItemStatus.Draft;

                // Process update change to DB
                if (await _unitOfWork.SaveChangesAsync() > 0) // Success
                {
                    // Initialize bool field
                    var isDeleted = false;
                    // Progress delete data in Elastic
                    if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.LibraryItemIndex))
                    {
                        // Check whether library item exist 
                        if (await _elasticService.Value.DocumentExistsAsync<ElasticLibraryItem>(
                                documentId: existingEntity.LibraryItemId.ToString()))
                        {
                            // Progress delete
                            isDeleted = await _elasticService.Value.DeleteAsync<ElasticLibraryItem>(
                                key: existingEntity.LibraryItemId.ToString());
                        }
                    }

                    // Custom message for failing to synchronize data to elastic
                    var msg = !isDeleted
                        ? isEng ? ", but fail to delete Elastic data" : ", nhưng xóa dữ liệu Elastic thất bại"
                        : string.Empty;
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
                }
            }

            // Mark as fail to save
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update library item status");
        }
    }

    public async Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Retrieve library item by id
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "library item to update borrow status"
                        : "tài liệu để sửa trạng thái có thể mượn"), false);
            }

            // Update status
            existingEntity.CanBorrow = canBorrow;

            // Progress update without change 
            await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(existingEntity);

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update item borrow status");
        }
    }

    public async Task<IServiceResult> UpdateShelfLocationAsync(int id, int? shelfId)
    {
        try
        {
            // Determine current lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                .Include(be => be.LibraryItemInstances)
            );

            // Retrieve library item by id
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"), false);
            }

            // Check exist shelf location
            var existingShelf = (await _libShelfService.AnyAsync(lf => lf.ShelfId == shelfId)).Data is true;
            if (!existingShelf && shelfId != null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "shelf location" : "kệ sách"));
            }
            else
            {
                // Check whether item already assigned to current shelf location
                if (existingEntity.ShelfId == shelfId) // same shelf location
                {
                    // Mark as update success
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
                }
            }

            // Required all book copy must be in out-of-shelf status
            if (existingEntity.LibraryItemInstances
                .Select(bec => bec.Status)
                .Any(status => status != nameof(LibraryItemInstanceStatus.OutOfShelf)))
            {
                // Msg: Cannot process, please move all edition copy status to inventory first
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0009));
            }

            // Process update shelf location
            existingEntity.ShelfId = shelfId;

            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }

            // Mark as update fail
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update shelf location for library item");
        }
    }
    
    public async Task<IServiceResult> UpdateGroupIdAsync(List<int> libraryItemIds, int newGroupId)
    {
        foreach (var libraryItemId in libraryItemIds)
        {
            var item = await _unitOfWork.Repository<LibraryItem,int>().GetByIdAsync(libraryItemId);
            if (item is null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "item"));
            }
            item.GroupId = newGroupId;
            await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(item);
        }

        var isSuccess = await _unitOfWork.SaveChangesAsync();
        if (isSuccess < 1)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
    }

    public async Task<IServiceResult> UpdateTrainingStatusAsync(List<int> libraryItemIds)
    {
        foreach (var libraryItemId in libraryItemIds)
        {
            var item = await _unitOfWork.Repository<LibraryItem,int>().GetByIdAsync(libraryItemId);
            if (item is null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "item"));
            }

            if (item.IsTrained)
            {
                continue;
            }
            item.IsTrained = true;
            item.TrainedAt = DateTime.Now;
            await _unitOfWork.Repository<LibraryItem, int>().UpdateAsync(item);
        }

        var isSuccess = await _unitOfWork.SaveChangesAsync();
        if (isSuccess < 1)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        return new ServiceResult(ResultCodeConst.SYS_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
    }
    
    public async Task<IServiceResult> SoftDeleteAsync(int id)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == id);
            // Include all constraints to update soft delete
            baseSpec.ApplyInclude(q => q
                // Ignore include all authors, due to library item can exist without any item belongs to    
                // Include all library item copy
                .Include(li => li.LibraryItemInstances)
                // Include all library resource
                .Include(li => li.LibraryItemResources)
                    .ThenInclude(lir => lir.LibraryResource)
            );
            // Get library item with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAsync(baseSpec);
            // Check if library item already mark as deleted
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
            }

            // Require transitioning to Draft status to modify or soft-delete a book
            if (existingEntity.Status != LibraryItemStatus.Draft)
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
            }

            // Check whether library item contains any item instances, which mark as not deleted
            if (existingEntity.LibraryItemInstances.Any(x => !x.IsDeleted))
            {
                // Extract all current item instance ids
                var itemInstanceIds = existingEntity.LibraryItemInstances
                    .Where(bec => !bec.IsDeleted)
                    .Select(bec => bec.LibraryItemInstanceId).ToList();
                if (itemInstanceIds.Any()) // Found any copy is not deleted yet
                {
                    // Try to softly delete all related edition copies
                    var deleteResult = await _itemInstanceService.Value.SoftDeleteRangeAsync(
                        libraryItemId: existingEntity.LibraryItemId,
                        libraryItemInstanceIds: itemInstanceIds);
                    // Check whether fail to delete range library item instances
                    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
                }
            }
            
            // Check whether library item contains any resource, which mark as not deleted
            if (existingEntity.LibraryItemResources.Select(lir =>
                    lir.LibraryResource).Any(x => !x.IsDeleted)
               )
            {
                // Extract all current resource ids
                var itemResourceIds = existingEntity.LibraryItemResources
                    .Select(lir => lir.LibraryResource)
                    .Where(lr => !lr.IsDeleted).Select(lr => lr.ResourceId).ToArray();
                if (itemResourceIds.Any())
                {
                    // Try to softly delete all related resources
                    var deleteResult = await _resourceService.Value.SoftDeleteRangeAsync(itemResourceIds); 
                    // Check whether fail to delete range library resources
                    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
                }
            }

            // Update delete status
            existingEntity.IsDeleted = true;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
        }
        catch (UnprocessableEntityException)
        {
            return new ServiceResult(ResultCodeConst.LibraryItem_Warning0008,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process soft delete library item");
        }
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => ids.Contains(x.LibraryItemId));
            // Include all constraints to update soft delete
            baseSpec.ApplyInclude(q => q
                // Ignore include all authors, due to library item can exist without any item belongs to    
                // Include all library item instance
                .Include(be => be.LibraryItemInstances)
                // Include all library resource
                .Include(li => li.LibraryItemResources)
                    .ThenInclude(lir => lir.LibraryResource)
            );
            var itemEntities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var itemList = itemEntities.ToList();
            if (itemList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            else if (itemList.Any(x => x.Status != LibraryItemStatus.Draft))
            {
                // Require transitioning to Draft status to modify or soft-delete a item
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
            }

            // Iterate each item to softly delete all instances, resources
            foreach (var item in itemList)
            {
                // Extract all current item instance ids
                var itemInstanceIds = item.LibraryItemInstances
                    .Where(bec => !bec.IsDeleted)
                    .Select(bec => bec.LibraryItemInstanceId).ToList();
                if (itemInstanceIds.Any())
                {
                    // Try to soft delete all related item instances
                    var deleteResult = await _itemInstanceService.Value.SoftDeleteRangeAsync(
                        libraryItemId: item.LibraryItemId,
                        libraryItemInstanceIds: itemInstanceIds);
                    // Check whether fail to delete range library item instances
                    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
                }
                
                // Extract all current item resource ids
                var itemResourceIds = item.LibraryItemResources
                    .Select(lir => lir.LibraryResource)
                    .Where(lr => !lr.IsDeleted)
                    .Select(lr => lr.ResourceId).ToArray();
                if (itemResourceIds.Any())
                {
                    // Try to softly delete all related resources
                    var deleteResult = await _resourceService.Value.SoftDeleteRangeAsync(itemResourceIds);
                    // Check whether fail to delete range library item instances
                    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
                }
                
                // Update deleted status
                item.IsDeleted = true;
            }
            
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when remove range library item");
        }
    }

    public async Task<IServiceResult> UndoDeleteAsync(int id)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == id);
            // Include all constraints to update soft delete
            baseSpec.ApplyInclude(q => q
                // Ignore include all authors, due to library item can exist without any item belongs to    
                // Include all library item instance
                .Include(be => be.LibraryItemInstances)
                // Include all library resource
                .Include(li => li.LibraryItemResources)
                    .ThenInclude(lir => lir.LibraryResource)
            );
            var existingEntity = await _unitOfWork.Repository<LibraryItem, int>()
                .GetWithSpecAsync(baseSpec);
            // Check if library item already mark as deleted
            if (existingEntity == null || !existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
            }

            // Check whether library item contains any item instance, which mark as deleted
            if (existingEntity.LibraryItemInstances.Any(x => x.IsDeleted))
            {
                // Extract all current edition copy ids
                var itemInstanceIds = existingEntity.LibraryItemInstances
                    .Where(bec => bec.IsDeleted)
                    .Select(bec => bec.LibraryItemInstanceId).ToList();
                if (itemInstanceIds.Any())
                {
                    // Try to undo all related item instances
                    var undoResult = await _itemInstanceService.Value.UndoDeleteRangeAsync(
                        libraryItemId: existingEntity.LibraryItemId,
                        libraryItemInstanceIds: itemInstanceIds);
                    // Check whether fail to undo range library item instances
                    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
                }
            }
            
            // Check whether library item contains any resource, which mark as deleted
            if (existingEntity.LibraryItemResources.Select(lir =>
                    lir.LibraryResource).Any(x => x.IsDeleted)
               )
            {
                // Extract all current resource ids
                var itemResourceIds = existingEntity.LibraryItemResources
                    .Select(lir => lir.LibraryResource)
                    .Where(lr => lr.IsDeleted).Select(lr => lr.ResourceId).ToArray();
                if (itemResourceIds.Any())
                {
                    // Try to undo delete all related resources
                    var undoResult = await _resourceService.Value.UndoDeleteRangeAsync(itemResourceIds); 
                    // Check whether fail to undo delete range library resources
                    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
                }
            }

            // Update delete status
            existingEntity.IsDeleted = false;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete library item");
        }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(x => ids.Contains(x.LibraryItemId));
            // Include all constraints to update soft delete
            baseSpec.ApplyInclude(q => q
                // Ignore include all authors, due to library item can exist without any item belongs to    
                // Include all library item instance
                .Include(be => be.LibraryItemInstances)
                // Include all library resource
                .Include(li => li.LibraryItemResources)
                    .ThenInclude(lir => lir.LibraryResource)
            );
            // Retrieve all data with spec
            var itemEntities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var itemList = itemEntities.ToList();
            if (itemList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Iterate each item to undo delete all instances, resources
            foreach (var item in itemList)
            {
                // Extract all current item instance ids
                var itemInstanceIds = item.LibraryItemInstances
                    .Where(bec => bec.IsDeleted)
                    .Select(bec => bec.LibraryItemInstanceId).ToList();
                if (itemInstanceIds.Any())
                {
                    // Try to soft undo all related edition copies
                    var undoResult = await _itemInstanceService.Value.UndoDeleteRangeAsync(
                        libraryItemId: item.LibraryItemId,
                        libraryItemInstanceIds: itemInstanceIds);
                    // Check whether fail to undo range library item copies
                    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
                }
                
                // Extract all current resource ids
                var itemResourceIds = item.LibraryItemResources
                    .Select(lir => lir.LibraryResource)
                    .Where(lr => lr.IsDeleted).Select(lr => lr.ResourceId).ToArray();
                if (itemResourceIds.Any())
                {
                    // Try to undo delete all related resources
                    var undoResult = await _resourceService.Value.UndoDeleteRangeAsync(itemResourceIds); 
                    // Check whether fail to undo delete range library resources
                    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
                }
                
                 // Update deleted status
                item.IsDeleted = false;
            }
            
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), true);
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete range");
        }
    }

    public async Task<IServiceResult> DeleteRangeAsync(int[] ids)
    {
        try
        {
            // Get all matching library item 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItem>(e => ids.Contains(e.LibraryItemId));
            // Apply including authors
            baseSpec.ApplyInclude(q => q
                .Include(be => be.LibraryItemAuthors));
            // Get all author with specification
            var itemEntities = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var itemList = itemEntities.ToList();
            if (itemList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Try to clear all authors existing in each of library item (if any)
            foreach (var be in itemList)
            {
                // Process delete range without save changes
                await _itemAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
                    // Select all existing libraryItemAuthorId
                    be.LibraryItemAuthors.Select(ba => ba.LibraryItemAuthorId).ToArray());
            }

            // Process delete range, and delete cascade with BookEditionInventory
            await _unitOfWork.Repository<LibraryItem, int>().DeleteRangeAsync(ids);

            // Save to DB
            if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, itemList.Count.ToString()), true);
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range library item");
        }
    }

    public async Task<IServiceResult> ExportAsync(ISpecification<LibraryItem> spec)
    {
	    try
	    {
		    // Try to parse specification to LibraryItemSpecification
		    var baseSpec = spec as LibraryItemSpecification;
		    // Check if specification is null
		    if (baseSpec == null)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }				
			
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(be => be.Shelf)
                .Include(be => be.Category)
                .Include(be => be.LibraryItemAuthors)
                    .ThenInclude(bea => bea.Author)
                .Include(be => be.LibraryItemInstances)
		    );
		    // Get all with spec
		    var entities = await _unitOfWork.Repository<LibraryItem, int>()
			    .GetAllWithSpecAsync(baseSpec, tracked: false);
		    if (entities.Any()) // Exist data
		    {
			    // Map entities to dtos 
			    var bookEditionDtos = _mapper.Map<List<LibraryItemDto>>(entities);
			    // Process export data to file
			    var fileBytes = CsvUtils.ExportToExcelWithNameAttribute(
				    bookEditionDtos.ToLibraryItemCsvRecords());

			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				    fileBytes);
		    }
			
		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process export library items");
	    }
    }

    private async Task<IServiceResult> DetectWrongImportDataInternalAsync<TCsvRecord>(
        int startRowIndex,
        List<TCsvRecord> records,
        List<string> coverImageNames) where TCsvRecord : LibraryItemCsvRecordDto
    {
        // Determine current system language
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;

	    // Initialize dictionary to hold errors
	    var errorMessages = new Dictionary<int, List<string>>();
	    // Default row index set to second row, as first row is header
	    var currDataRow = startRowIndex;

	    foreach (var record in records)
	    {
	        // Initialize error list for the current row
	        var rowErrors = new List<string>();
            
	        // Check exist cover image
	        if (!coverImageNames.Exists(str => str.Equals(record.CoverImage)))
	        {
	            rowErrors.Add(isEng ? $"Image file name '{record.CoverImage}' does not exist" : $"Không tìm thấy file hình có tên '{record.CoverImage}'");
	        }

	        // Check exist shelf location
	        // var isExistShelfLocation = (await _libShelfService.AnyAsync(x => x.ShelfNumber == record.ShelfNumber)).Data is true;
	        // if (!isExistShelfLocation)
	        // {
	        //     rowErrors.Add(isEng ? $"Shelf number '{record.ShelfNumber}' does not exist" : $"Kệ số '{record.ShelfNumber}' không tồn tại");
	        // }

	        // Check exist author code
            var isExistAuthor = (await _authorService.AnyAsync(x => !string.IsNullOrEmpty(record.AuthorCode) &&
                x.AuthorCode.ToLower() == record.AuthorCode.ToLower())).Data is true;
            if (!isExistAuthor)
            {
                rowErrors.Add(isEng ? $"Author code '{record.AuthorCode}' does not exist" : $"Mã tác giả '{record.AuthorCode}' không tồn tại");
            }
			
            // Check exist category
            var categoryDto = 
                (await _cateService.GetWithSpecAsync(new BaseSpecification<Category>(
                x => x.VietnameseName == record.Category 
                     || x.EnglishName == record.Category))
                ).Data as CategoryDto;
            if (categoryDto == null)
            {
                rowErrors.Add(isEng
                    ? $"Category '{record.Category}' does not exist"
                    : $"Thể loại '{record.Category}' không tồn tại");
            }
            
            /*
		    // Check exist item instances
		    var itemInstanceLength = record.ItemInstanceBarcodes != null ? record.ItemInstanceBarcodes.Split(",").Length : 0;
		    if (itemInstanceLength > 0)
		    {
			    // Split elements by comma
                var itemInstanceBarcodes = record.ItemInstanceBarcodes!.Split(",")
                    .Select(str => str.Trim())
                    .ToList();
                
			    // Group code and check duplicate
			    var duplicateBarcodes = itemInstanceBarcodes.GroupBy(x => x)
				    .Where(g => g.Count() > 1)
				    .Select(g => g.Key)
				    .ToList();

			    // Check whether code is duplicate within a single cell
			    if (duplicateBarcodes.Any())
			    {
				   rowErrors.Add(isEng
					   ? $"The following barcodes are duplicated: {string.Join(", ", duplicateBarcodes)}"
					   : $"Các mã barcode sau đây bị trùng lặp: {string.Join(", ", duplicateBarcodes)}");
			    }
			    
			    foreach (var barcode in itemInstanceBarcodes)
			    {
			        // Check whether code is duplicate within all other cells
				    if (!itemBarcodeSet.Add(barcode)) 
				    {
					    rowErrors.Add(isEng
						    ? $"Barcode '{barcode}' already exists in file"
						    : $"Barcode '{barcode}' bị trùng trong file");
				    }
                    
			        // Check whether barcode already exist in DB
                    var isExist = (await _itemInstanceService.Value.AnyAsync(x => x.Barcode.ToLower() == barcode.ToLower())).Data is true;
                    if (isExist)
                    {
                        rowErrors.Add(isEng
                            ? $"Barcode '{barcode}' already not exist"
                            : $"Barcode '{barcode}' đã tồn tại");
                    }
                    
                    // Try to validate with category prefix
                    var isValidBarcode =
                        StringUtils.IsValidBarcodeWithPrefix(barcode, categoryDto!.Prefix);
                    if (!isValidBarcode)
                    {
                        rowErrors.Add(isEng
                            ? $"The prefix of barcode is invalid, the prefix pattern of the category is {categoryDto.Prefix}"
                            : $"Tiền tố của số đăng ký cá biệt không hợp lệ, mẫu tiền tố của thể loại là {categoryDto.Prefix}");
                    }
			    }
		    }
            */
            
            // Check exist ISBN
            var isExistIsbn = await _unitOfWork.Repository<LibraryItem, int>()
                .AnyAsync(be => !string.IsNullOrEmpty(record.Isbn) && be.Isbn == ISBN.CleanIsbn(record.Isbn));
            if (isExistIsbn)
            {
                rowErrors.Add(isEng ? $"ISBN '{record.Isbn}' already exists" : $"Mã ISBN '{record.Isbn}' đã tồn tại");
            }
            else // Check duplicate ISBN
            {
                var isbnList = records.Select(x => x.Isbn).ToList();
                var isIsbnDuplicate = isbnList.Count(x => x == record.Isbn) > 1;
                if (isIsbnDuplicate)
                {
                    rowErrors.Add(isEng ? $"IBSN '{record.Isbn}' is duplicated" : $"Mã ISBN '{record.Isbn}' bị trùng");
                }
            }
		    
		    // Validations
            if (string.IsNullOrEmpty(record.Title)) // Title
            {
                rowErrors.Add(isEng
                    ? "Title is required" 
                    : "Yêu cầu nhập tiêu đề cho tài liệu");
            }
            else if (record.Title.Length > 150) 
		    {
			    rowErrors.Add(isEng
				    ? "Item title must not exceed than 150 characters"
				    : "Tiêu đề của tài liệu phải nhỏ hơn 150 ký tự");
		    }
            
            if (record.SubTitle?.Length > 150) // SubTitle
            {
                rowErrors.Add(isEng
                    ? "Item subtitle must not exceed than 150 characters"
                    : "Tiêu đề phụ của tài liệu phải nhỏ hơn 150 ký tự");
            }
            
            if (record.Responsibility?.Length > 150) // Responsibility
            {
                rowErrors.Add(isEng
                    ? "Statement of responsibility must not exceed than 155 characters"
                    : "Thông tin trách nhiệm của tài liệu phải nhỏ hơn 155 ký tự");
            }
            
            if (record.Edition?.Length > 100) // Edition
            {
                rowErrors.Add(isEng
                    ? "Edition must not exceed than 100 characters"
                    : "Thông tin lần xuất bản phải nhỏ hơn 150 ký tự");
            }
            
            if (record.EditionNumber <= 0 && record.PageCount < int.MaxValue) // Edition number
		    {
			    rowErrors.Add(isEng
				    ? "Item edition number is not valid"
				    : "Số thứ tự tài liệu không hợp lệ");
		    }
            
            if (string.IsNullOrEmpty(record.Language)) // Language
            {
                rowErrors.Add(isEng
                    ? "Language is required" 
                    : "Yêu cầu xác định ngôn ngữ cho tài liệu");
            }
            else if (record.Language.Length > 50) 
            {
                rowErrors.Add(isEng
                    ? "Language must not exceed than 50 characters"
                    : "Ngôn ngữ phải nhỏ hơn 50 ký tự");
            }
            
            if (record.OriginLanguage?.Length > 50) // Origin language
            {
                rowErrors.Add(isEng
                    ? "Origin language must not exceed than 50 characters"
                    : "Ngôn ngữ gốc phải nhỏ hơn 50 ký tự");
            }

            if (record.Summary?.Length > 500) // Summary
            {
                rowErrors.Add(isEng
                    ? "Item summary must not exceed 500 characters"
                    : "Mô tả của tài liệu không vượt quá 500 ký tự");
            }
            
            if (!(int.TryParse(record.PublicationYear.ToString(), out var year) 
		        && year > 0 && year <= DateTime.Now.Year)) // Publication year
		    {
			    rowErrors.Add(isEng
				    ? "Publication year is not valid"
				    : "Năm xuất bản không hợp lệ");
		    }
            
            if (record.Publisher.Length > 255) // Publisher
            {
                rowErrors.Add(isEng
                    ? "Publisher must not exceed than 255 characters"
                    : "Tên nhà xuất bản phải nhỏ hơn 255 ký tự");
            }
            else if (StringUtils.IsDateTime(record.Publisher)) 
            {
                rowErrors.Add(isEng
                    ? "Publisher is not valid"
                    : "Tên nhà xuất bản không hợp lệ");
            }
            
            if (record.PublicationPlace.Length > 255) // Publication place
            {
                rowErrors.Add(isEng
                    ? "Publication place must not exceed 255 characters"
                    : "Nơi xuất bản không vượt quá 255 ký tự");
            }
            else if (StringUtils.IsNumeric(record.PublicationPlace) || 
                StringUtils.IsDateTime(record.PublicationPlace)) 
            {
                rowErrors.Add(isEng
                    ? "Publication place is not include number or date"
                    : "Thông tin nơi xuất bản không bao gồm số hoặc ngày");
            }
            
            
            if (record.ClassificationNumber.Length > 50) // DDC number
            {
                rowErrors.Add(isEng
                    ? "DDC number must not exceed than 50 characters"
                    : "Mã DDC tài liệu phải nhỏ hơn 50 ký tự");
            }
            else if (!DeweyDecimalUtils.IsValidDeweyDecimal(record.ClassificationNumber)
                     || StringUtils.IsDateTime(record.ClassificationNumber))
            {
                rowErrors.Add(isEng
                    ? "DDC number is not valid"
                    : "Mã DDC tài liệu không hợp lệ");
            }
            
            if (record.CutterNumber.Length > 50) // CutterNumber
            {
                rowErrors.Add(isEng
                    ? "Cutter number must not exceed than 50 characters"
                    : "Ký hiệu xếp giá phải nhỏ hơn 50 ký tự");
            }
            else if (!DeweyDecimalUtils.IsValidCutterNumber(record.CutterNumber)
                     || StringUtils.IsDateTime(record.CutterNumber))
            {
                rowErrors.Add(isEng
                    ? "Cutter number is not valid"
                    : "Ký hiệu xếp giá không hợp lệ");
            }

            if (ISBN.CleanIsbn(record.Isbn ?? string.Empty).Length > 13) // Isbn
            {
                rowErrors.Add(isEng
                    ? "ISBN must not exceed 13 characters"
                    : "Mã ISBN không vượt quá 13 ký tự");
            }else if (!ISBN.IsValid(record.Isbn ?? string.Empty, out _))
            {
                rowErrors.Add(isEng
                    ? "ISBN is not valid"
                    : "Mã ISBN không hợp lệ");
            }
            
            if (record.EstimatedPrice < 1000 || record.EstimatedPrice > 9999999999) // Estimated price
            {
                if (record.EstimatedPrice < 1000)
                {
                    rowErrors.Add(isEng
                        ? "EstimatedPrice must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND");
                }
                else if (record.EstimatedPrice > 9999999999)
                {
                    rowErrors.Add(isEng
                        ? "EstimatedPrice exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND");
                }
            }
            
            if (record.PageCount <= 0 && record.PageCount < int.MaxValue) // Page count
            {
                rowErrors.Add(isEng
                    ? "Page count is not valid"
                    : "Tổng số trang không hợp lệ");
            }
            
            if (record.PhysicalDetails?.Length > 100) // Title
            {
                rowErrors.Add(isEng
                    ? "Physical detail must not exceed 100 characters"
                    : "Các đặc điểm vật lý khác không vượt quá 100 ký tự");
            }

            if (string.IsNullOrEmpty(record.Dimensions))
            {
                rowErrors.Add(isEng
                    ? "Dimensions is required" 
                    : "Yêu cầu xác định mô tả kích thước cho tài liệu");
            }
            else if (record.Dimensions.Length > 50) // Dimensions
            {
                rowErrors.Add(isEng
                    ? "Dimensions must not exceed 50 characters"
                    : "Mô tả kích thước không vượt quá 50 ký tự");
            }
            
            if (record.Genres?.Length > 255) // Genres
            {
                rowErrors.Add(isEng
                    ? "Genres must not exceed 255 characters"
                    : "Chủ đề thể loại/hình thức không vượt quá 255 ký tự");
            }
            
            if (record.GeneralNote?.Length > 100) // General note
            {
                rowErrors.Add(isEng
                    ? "General note must not exceed 100 characters"
                    : "Phụ chú chung không vượt quá 100 ký tự");
            }
            
            if (record.BibliographicalNote?.Length > 100) // Bibliographical note
            {
                rowErrors.Add(isEng
                    ? "Bibliographical note must not exceed 100 characters"
                    : "Phụ chú thư mục không vượt quá 100 ký tự");
            }
            
            if (record.TopicalTerms?.Length > 500) // Topical terms
            {
                rowErrors.Add(isEng
                    ? "Topical terms must not exceed 500 characters"
                    : "Chủ đề có kiểm soát không vượt quá 500 ký tự");
            }
            
            if (record.AdditionalAuthors?.Length > 500) // Additional authors
            {
                rowErrors.Add(isEng
                    ? "Additional authors must not exceed 500 characters"
                    : "Tác giả bổ sung không vượt quá 500 ký tự");
            }
            
	        // if errors exist for the row, add to the dictionary
	        if (rowErrors.Any())
	        {
	            errorMessages.Add(currDataRow, rowErrors);
	        }

	        // Increment the row counter
	        currDataRow++;
	    }

	    return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), errorMessages);
    }
    
    async Task<IServiceResult> ILibraryItemService<LibraryItemDto>.DetectWrongImportDataAsync<TCsvRecord>(
        int startRowIndex,
        List<TCsvRecord> records, 
        List<string> coverImageNames)
    {
        return await DetectWrongImportDataInternalAsync(
            startRowIndex: startRowIndex,
            records: records.Cast<LibraryItemCsvRecordDto>().ToList(),
            coverImageNames: coverImageNames);
    }
    
    private async Task<IServiceResult> DetectDuplicatesInFileInternalAsync<TCsvRecord>(
        List<TCsvRecord> records, string[] scanningFields) where TCsvRecord : LibraryItemCsvRecordDto
    {
        // Determine current system language
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        // Check whether exist any scanning fields
        if (scanningFields.Length == 0)
        {
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                new
                {
                    Errors = new Dictionary<int, List<string>>(),
                    Duplicates = new Dictionary<int, List<int>>()
                });
        }

        // Initialize error messages (for display purpose)
        var errorMessages = new Dictionary<int, List<string>>();
        
        // Initialize key pair dictionary (for handle purpose)
        // Key: root element
        // Value: duplicate elements with root
        var duplicates = new Dictionary<int, List<int>>();
        
        // Initialize a map to track seen keys for each field
        var fieldToSeenKeys = new Dictionary<string, Dictionary<string, int>>();
        foreach (var field in scanningFields.Select(f => f.ToUpperInvariant()))
        {
            fieldToSeenKeys[field] = new Dictionary<string, int>();
        }

        // Default row index set to second row, as first row is header
        var currDataRow = 2;
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            
            // Initialize row errors
            var rowErrors = new List<string>();
            
            // Check duplicates for each scanning field
            foreach (var field in scanningFields.Select(f => f.ToUpperInvariant()))
            {
                string? fieldValue = field switch
                {
                    var title when title == nameof(LibraryItem.Title).ToUpperInvariant() => record.Title?.Trim()
                        .ToUpperInvariant(),
                    var coverImage when coverImage == nameof(LibraryItem.CoverImage).ToUpperInvariant() => record.CoverImage
                        ?.Trim().ToUpperInvariant(),
                    _ => null
                };

                // Skip if the field value is null or empty
                if (string.IsNullOrEmpty(fieldValue))
                    continue;

                // Check if the key has already seen
                var seenKeys = fieldToSeenKeys[field];
                if (seenKeys.ContainsKey(fieldValue))
                {
                    // Retrieve the first index where the duplicate was seen
                    var firstItemIndex = seenKeys[fieldValue];

                    // Add the current index to the duplicates list
                    if (!duplicates.ContainsKey(firstItemIndex))
                    {
                        duplicates[firstItemIndex] = new List<int>();
                    }

                    duplicates[firstItemIndex].Add(i);

                    // Add duplicate error message
                    rowErrors.Add(isEng
                        ? $"Duplicate data for field '{field}': '{fieldValue}'"
                        : $"Dữ liệu bị trùng cho trường '{field}': '{fieldValue}'");
                }
                else
                {
                    // Mark this field value as seen at the current index
                    seenKeys[fieldValue] = i;
                }
            }
            
            // If errors exist for specific row, add to the dictionary
            if (rowErrors.Any())
            {
                errorMessages.Add(currDataRow, rowErrors);
            }
            
            // Increment the row counter
            currDataRow++;
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            new
            {
                Errors = errorMessages,
                Duplicates = duplicates
            });
    }

    async Task<IServiceResult> ILibraryItemService<LibraryItemDto>.DetectDuplicatesInFileAsync<TCsvRecord>(
        List<TCsvRecord> records, string[] scanningFields)
    {
        return await DetectDuplicatesInFileInternalAsync(
            records: records.Cast<LibraryItemCsvRecordDto>().ToList(),
            scanningFields: scanningFields);
    }

    #region Archived Code Statement
    /*
    public async Task<IServiceResult> ImportAsync(
	    IFormFile? file, 
	    List<IFormFile> coverImageFiles,
	    string[]? scanningFields,
        DuplicateHandle? duplicateHandle = null)
    {
	    try
	    {
		    // Determine system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
			    .CurrentLanguage);

		    // Check exist file
		    if (file == null || file.Length == 0)
		    {
			    return new ServiceResult(ResultCodeConst.File_Warning0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
		    }

		    // Validate import file 
		    var validationResult = await ValidatorExtensions.ValidateAsync(file);
		    if (validationResult != null && !validationResult.IsValid)
		    {
			    // Response the uploaded file is not supported
                return new ServiceResult(ResultCodeConst.File_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
		    }

		    // Csv config
		    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
		    {
			    HasHeaderRecord = true,
			    HeaderValidated = null,
			    MissingFieldFound = null
		    };

		    // Process read csv file
		    var readResp =
			    CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<LibraryItemCsvRecordDto>(
                    file: file,
                    config: csvConfig,
                    props: new ExcelProps()
                    {
                        // Header start from row 1-1
                        FromRow = 1,
                        ToRow = 1,
                        // Start from col
                        FromCol = 1,
                        // Start read data index
                        StartRowIndex = 2
                    },
                    encodingType: null,
                    systemLang: lang);
			if(readResp.Errors.Any())
			{
				var errorResps = readResp.Errors.Select(x => new ImportErrorResultDto()
				{	
					RowNumber = x.Key,
					Errors = x.Value.ToList()
				});
			    
				return new ServiceResult(ResultCodeConst.SYS_Fail0008,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
			}
		    
		    // Extract all cover image file name
		    var imageFileNames = coverImageFiles.Select(f => f.FileName).ToList();
		    // Find duplicate image file names
		    var duplicateFileNames = imageFileNames
			    .GroupBy(name => name)
			    .Where(group => group.Count() > 1) // Filter groups with more than one occurrence
			    .Select(group => group.Key)       // Select the duplicate file names
			    .ToList();
		    if (duplicateFileNames.Any())
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0004);
			    
			    // Add single quotes to each file name
			    var formattedFileNames = duplicateFileNames
				    .Select(fileName => $"'{fileName}'"); 

			    return new ServiceResult(
				    ResultCodeConst.File_Warning0004,
				    StringUtils.Format(errMsg, String.Join(", ", formattedFileNames))
			    );
		    }
			
		    // Detect record errors
            if ((await DetectWrongImportDataInternalAsync(
                    startRowIndex: 2, readResp.Records, imageFileNames)
                ).Data is Dictionary<int, List<string>> detectResult && detectResult.Any())
		    {
			    var errorResps = detectResult.Select(x => new ImportErrorResultDto()
			    {	
					RowNumber = x.Key,
					Errors = x.Value
			    });
			    
			    return new ServiceResult(ResultCodeConst.SYS_Fail0008,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
		    }
            
            // Additional message
            var additionalMsg = string.Empty;
            // Detect duplicates
            var detectDuplicateResult = DetectDuplicatesInFileInternal(readResp.Records, scanningFields ?? [], lang);
            if (detectDuplicateResult.Errors.Count != 0 && duplicateHandle == null) // Has not selected any handle options yet
            {
                var errorResp = detectDuplicateResult.Errors.Select(x => new ImportErrorResultDto()
                {	
                    RowNumber = x.Key,
                    Errors = x.Value
                });
                
                // Response error messages for data confirmation and select handle options 
                return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResp);
            }
            if (detectDuplicateResult.Errors.Count != 0 && duplicateHandle != null) // Selected any handle options
            {
                // Handle duplicates
                var handleResult = CsvUtils.HandleDuplicates(
                    readResp.Records, detectDuplicateResult.Duplicates, (DuplicateHandle) duplicateHandle, lang);
                // Update records
                readResp.Records = handleResult.handledRecords;
                // Update msg 
                additionalMsg = handleResult.msg;
            }
            
		    // Handle upload images (Image name | URL)
		    var uploadFailList = new List<string>();
		    var imageUrlDic = new Dictionary<string, string>();
		    foreach (var coverImage in coverImageFiles)
		    {
			    // Try to validate file
			    var validateResult = await 
				    new ImageTypeValidator(lang.ToString() ?? SystemLanguage.English.ToString()).ValidateAsync(coverImage);
			    if (!validateResult.IsValid)
			    {
				    var isEng = lang == SystemLanguage.English;
				    return new ServiceResult(ResultCodeConst.SYS_Warning0001, isEng 
					    ? $"File '{coverImage.FileName}' is not a image file " +
					      $"Valid format such as (.jpeg, .png, .gif, etc.)" 
					    : $"File '{coverImage.FileName}' không phải là file hình ảnh. " +
					      $"Các loại hình ảnh được phép là: (.jpeg, .png, .gif, v.v.)");
			    }
			    
			    // Upload image to cloudinary
			    var uploadResult = (await _cloudService.UploadAsync(coverImage, FileType.Image, ResourceType.BookImage))
				    .Data as CloudinaryResultDto;
			    if (uploadResult == null)
			    {
				    // Add image that fail to upload
				    uploadFailList.Add(coverImage.FileName);
			    }
			    else
			    {
				    // Add to dic
				    imageUrlDic.Add(coverImage.FileName, uploadResult.SecureUrl);
			    }
		    }

		    var totalImported = 0;
		    var totalFailed = 0;
		    // Process import book editions
		    var successRecords = readResp.Records
			    .Where(r => !uploadFailList.Contains(r.CoverImage))
			    .ToList();
		    var failRecords = new List<LibraryItemCsvRecordDto>();
		    if (successRecords.Any())
		    {
				// Initialize list items
				var itemList = new List<LibraryItemDto>();
			    foreach (var record in successRecords)
			    {
				    // Extract all item instance barcodes
				    var itemInstanceBarcodes = !string.IsNullOrWhiteSpace(record.ItemInstanceBarcodes)
					    ? record.ItemInstanceBarcodes.Split(",")
                            .Select(str => str.Trim())
                            .Select(barcode => new LibraryItemInstanceDto()
					    {
						    Barcode = barcode,
						    IsDeleted = false,
						    Status = nameof(LibraryItemInstanceStatus.OutOfShelf)
					    }).ToList()
					    : new List<LibraryItemInstanceDto>();
                    
                    // Get author by code
                    var authorDto = (await _authorService.GetWithSpecAsync(new BaseSpecification<Author>(a =>
                            record.AuthorCode != null &&
                            Equals(a.AuthorCode.ToLower(), record.AuthorCode.ToLower())))
                        ).Data as AuthorDto;
                    
					// Get shelf location
					var shelfDto = 
						(await _libShelfService.GetWithSpecAsync(new BaseSpecification<LibraryShelf>(
						s => record.ShelfNumber != null && s.ShelfNumber.ToLower() == record.ShelfNumber.ToLower()))
						).Data as LibraryShelfDto;
                    
                    // Get category
                    var categoryDto = (await _cateService.GetWithSpecAsync(new BaseSpecification<Category>(
                            x => Equals(x.VietnameseName, record.Category) || Equals(x.EnglishName, record.Category)))
                        ).Data as CategoryDto;
                    
                    // Add new item
                    itemList.Add(new LibraryItemDto()
                    {
                        // Cover image
                        CoverImage = imageUrlDic.TryGetValue(record.CoverImage, out var coverImageUrl)
                            ? coverImageUrl
                            : null,
                        // Title
                        Title = record.Title,
                        // SubTitle
                        SubTitle = record.SubTitle,
                        // Responsibility
                        Responsibility = record.Responsibility,
                        // Edition
                        Edition = record.Edition,
                        // Edition
                        EditionNumber = record.EditionNumber,
                        // Language
                        Language = record.Language,
                        // OriginLanguage
                        OriginLanguage = record.OriginLanguage,
                        // Summary
                        Summary = record.Summary,
                        // PublicationYear
                        PublicationYear = record.PublicationYear,
                        // Publisher
                        Publisher = record.Publisher,
                        // PublicationPlace
                        PublicationPlace = record.PublicationPlace,
                        // ClassificationNumber
                        ClassificationNumber = record.ClassificationNumber,
                        // CutterNumber
                        CutterNumber = record.CutterNumber,
                        // ISBN
                        Isbn = record.Isbn,
                        // ISBN
                        EstimatedPrice = record.EstimatedPrice,
                        // PageCount
                        PageCount = record.PageCount,
                        // PhysicalDetails
                        PhysicalDetails = record.PhysicalDetails,
                        // Dimensions
                        Dimensions = record.Dimensions,
                        // Genres
                        Genres = record.Genres,
                        // GeneralNote
                        GeneralNote = record.GeneralNote,
                        // GeneralNote
                        BibliographicalNote = record.BibliographicalNote,
                        // TopicalTerms
                        TopicalTerms = record.TopicalTerms,
                        // AdditionalAuthors
                        AdditionalAuthors = record.AdditionalAuthors,
                        // Library shelf 
                        ShelfId = shelfDto?.ShelfId,
                        // Category
                        CategoryId = categoryDto!.CategoryId,
                        // Item instances
                        LibraryItemInstances = itemInstanceBarcodes.ToList(),
                        // Item authors
                        LibraryItemAuthors = authorDto != null! ? new List<LibraryItemAuthorDto>
                        {
                            new(){ AuthorId = authorDto.AuthorId }
                        } : new List<LibraryItemAuthorDto>(),
                        // Item inventory
                        LibraryItemInventory = new()
                        {
                            TotalUnits = itemInstanceBarcodes.Count, // Count total instances
                            AvailableUnits = 0,
                            BorrowedUnits = 0,
                            RequestUnits = 0,
                            ReservedUnits = 0
                        },
                        
                        // Default values
                        IsTrained = false,
                        IsDeleted = false,
                        CanBorrow = false,
                        Status = LibraryItemStatus.Draft,
                    });
			    }
                
				if (itemList.Any())
				{
					// Add new book
					await _unitOfWork.Repository<LibraryItem, int>().AddRangeAsync(_mapper.Map<List<LibraryItem>>(itemList));
					
					// Save change to DB
					if(await _unitOfWork.SaveChangesAsync() > 0) totalImported = itemList.Count;
					else failRecords.AddRange(successRecords);
				}
		    }
		    
		    // Aggregate all book editions fail to upload & fail to save DB (if any)
		    failRecords.AddRange(readResp.Records
			    .Where(r => uploadFailList.Contains(r.CoverImage))
			    .ToList());
		    if (failRecords.Any()) totalFailed = failRecords.Count;
			
		    string message;
		    byte[]? fileBytes;
			// Generate a message based on the import and failure counts
		    if (totalImported > 0 && totalFailed == 0)
		    {
			    // All records imported successfully
			    message = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005), totalImported.ToString());
                // Additional message (if any)
                message = !string.IsNullOrEmpty(additionalMsg) ? $"{message}, {additionalMsg}" : message;
                // Generate excel file for imported data
                fileBytes = CsvUtils.ExportToExcel(successRecords, sheetName: "ImportedItems");
			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
		    }

		    if (totalImported > 0 && totalFailed > 0)
		    {
			    // Partial success with some failures
			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadItems");

			    var baseMessage = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
                var failMessage = lang == SystemLanguage.English
				    ? $", {totalFailed} failed to import"
				    : $", {totalFailed} thêm mới thất bại";
                
			    message = StringUtils.Format(baseMessage, totalImported.ToString());
                // Additional message (if any)
                message = !string.IsNullOrEmpty(additionalMsg) ? $"{message}, {additionalMsg} {failMessage}" : message + failMessage;
			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
		    }

		    if (totalImported == 0 && totalFailed > 0)
		    {
			    // Complete failure
			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadItems");
			    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
			    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message, Convert.ToBase64String(fileBytes));
		    }

			// Default case: No records imported or failed
		    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
		    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message);
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process import library items");
	    }
    }
    */
    #endregion
}