using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
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
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemInstanceService : GenericService<LibraryItemInstance, LibraryItemInstanceDto, int>,
    ILibraryItemInstanceService<LibraryItemInstanceDto>
{
    // Lazy services    
    private readonly Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> _whTrackingDetailService;
    
    private readonly AppSettings _appSettings;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecService;
    private readonly ICategoryService<CategoryDto> _categoryService;
    private readonly ILibraryItemService<LibraryItemDto> _libItemService;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventoryService;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionService;
    private readonly ILibraryItemConditionHistoryService<LibraryItemConditionHistoryDto> _conditionHistoryService;

    public LibraryItemInstanceService(
        // Lazy services
        Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> whTrackingDetailService,
        
        IBorrowRecordService<BorrowRecordDto> borrowRecService,
        ICategoryService<CategoryDto> categoryService,
        ILibraryItemService<LibraryItemDto> libItemService,
        ILibraryItemInventoryService<LibraryItemInventoryDto> inventoryService,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionService,
        ILibraryItemConditionHistoryService<LibraryItemConditionHistoryDto> conditionHistoryService,
        IOptionsMonitor<AppSettings> monitor,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _appSettings = monitor.CurrentValue;
        _borrowRecService = borrowRecService;
        _conditionService = conditionService;
        _categoryService = categoryService;
        _libItemService = libItemService;
        _inventoryService = inventoryService;
        _whTrackingDetailService = whTrackingDetailService;
        _conditionHistoryService = conditionHistoryService;
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(x => x.LibraryItemInstanceId == id);
            // Apply including condition histories
            baseSpec.ApplyInclude(q => q.Include(li => li.LibraryItemConditionHistories));
            // Check exist item instance by id
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            // Get data success
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<LibraryItemInstanceDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get data");
        }
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build a base specification to filter by LibraryItemCopyId
            var baseSpec = new BaseSpecification<LibraryItemInstance>(a => a.LibraryItemInstanceId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(bec => bec.LibraryItemConditionHistories));

            // Retrieve library item copy with specification
            var itemInstanceEntity =
                await _unitOfWork.Repository<LibraryItemInstance, int>().GetWithSpecAsync(baseSpec);
            if (itemInstanceEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item copy" : "bản sao"));
            }

            // Check whether library item copy in the trash bin
            if (!itemInstanceEntity.IsDeleted)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Check whether total copy condition monitoring
            // history equal 1 (default book condition when create)
            if (itemInstanceEntity.LibraryItemConditionHistories.Count == 1)
            {
                // Progress delete without save
                await _conditionHistoryService.DeleteWithoutSaveChangesAsync(
                    // Retrieve first element id
                    itemInstanceEntity.LibraryItemConditionHistories.First().ConditionHistoryId);
            } // Else: do not allow to delete

            // Process add delete entity
            await _unitOfWork.Repository<LibraryItemInstance, int>().DeleteAsync(id);
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
        catch (InvalidOperationException ex)
        {
            _logger.Error(ex.Message);

            // Handle delete constraint data
            if (ex.Message.Contains("required relationship") || ex.Message.Contains("severed"))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
            }

            // Throw for other issues 
            throw new Exception("Error invoke when progress delete data");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress delete data");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, LibraryItemInstanceDto dto)
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
                throw new UnprocessableEntityException("Invalid validations", errors);
            }

            // Retrieve the entity
            // Build specification query
            var baseSpec = new BaseSpecification<LibraryItemInstance>(x => x.LibraryItemInstanceId == id);
            // Include borrow records and requests relation
            baseSpec.ApplyInclude(q => q
                // Include library item
                .Include(bec => bec.LibraryItem)
                    // Include library item inventory
                    .ThenInclude(li => li.LibraryItemInventory)
                // Include library item
                .Include(bec => bec.LibraryItem)
                    // Include category
                    .ThenInclude(li => li.Category)
            );
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item copy" : "bản sao"));
            }

            // Validate status
            Enum.TryParse(typeof(LibraryItemInstanceStatus), dto.Status, out var validStatus);
            if (validStatus == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "any status match to process update" : "trạng thái phù hợp"));
            }
            
            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Parse into enum
            var enumStatus = (LibraryItemInstanceStatus)validStatus;
            // Check if status change
            if (!Equals(existingEntity.Status, enumStatus.ToString())) // Change detected
            {
                // Msg: Cannot update item instance status as {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0022);
                
                // Do not allow to update BORROWED/RESERVED/LOST status
                // With RESERVED status of item instance, it will change automatically when 
                // someone return their borrowed book and assigned that book to others, who are in reservation queue
                if (enumStatus == LibraryItemInstanceStatus.Borrowed ||
                    enumStatus == LibraryItemInstanceStatus.Reserved ||
                    enumStatus == LibraryItemInstanceStatus.Lost)
                {
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0022,
                        StringUtils.Format(errMsg, isEng 
                                ? "selected status is invalid" 
                                : "trạng thái thay đổi không hợp lệ"));
                }
                else if (enumStatus == LibraryItemInstanceStatus.InShelf)
                {
                    // Required exist shelf location in library item for update to in-shelf status
                    if (existingEntity.LibraryItem.ShelfId == null || existingEntity.LibraryItem.ShelfId == 0)
                    {
                        errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                        // Required shelf location
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                            StringUtils.Format(errMsg, isEng
                                ? "Shelf location not found"
                                : "Không tìm thấy vị trí kệ cho tài liệu"));
                    }
                }

                var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(bRec => 
                            bRec.BorrowRecordDetails.Any(brd => 
                                brd.LibraryItemInstanceId == id && // Exist in any borrow record details
                                brd.Status != BorrowRecordStatus.Returned)) // Exclude elements with returned status
                    ).Data is true; // Convert object to boolean 
                
                if (hasBorrowRecordConstraint) // Has any constraint 
                {
                    // Cannot change data that is on borrowing or reserved
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0008,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008));
                }

                // Process update status
                existingEntity.Status = dto.Status;
            }
            
            // Check if barcode change
            if (!Equals(existingEntity.Barcode, dto.Barcode)) // Change detected
            {
                // Check exist barcode
                var isBarcodeExist = await _unitOfWork.Repository<LibraryItemInstance, int>()
                    .AnyAsync(li => 
                        Equals(li.Barcode.ToLower(), dto.Barcode.ToLower()) && // Same barcode format + suffix
                        !Equals(li.LibraryItemInstanceId == id) // Check with other item instances
                );
                if (isBarcodeExist)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0005);
                    // Add error
                    customErrs.Add(StringUtils.ToCamelCase(nameof(LibraryItemInstance.Barcode)),
                        [StringUtils.Format(errMsg, $"'{dto.Barcode}'")]);
                }
                else // Check valid barcode prefix
                {
                    // Retrieve category prefix
                    var catePrefix = existingEntity.LibraryItem.Category.Prefix;
                    // Check whether new barcode match prefix
                    var isMatched = StringUtils.IsValidBarcodeWithPrefix(dto.Barcode, catePrefix);
                    if (!isMatched) // Not matched
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
                        // Add error         
                        customErrs.Add(StringUtils.ToCamelCase(nameof(LibraryItemInstance.Barcode)), 
                            [StringUtils.Format(errMsg, $"'{catePrefix}'")]);
                    }
                }
                
                // Progress update barcode
                existingEntity.Barcode = dto.Barcode;
            }

            // Check if any errors invoked
            if (customErrs.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrs);
            }
            
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<LibraryItemInstance, int>().HasChanges(existingEntity))
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }

            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == existingEntity.LibraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }
            
            // Check for typeof status update
            if (enumStatus == LibraryItemInstanceStatus.OutOfShelf)
            {
                // Count total pending request
                var pendingRequestUnits = currentInventory.RequestUnits;
                // Count total in-shelf units
                var countSpec = new BaseSpecification<LibraryItemInstance>(li =>
                    li.Status == nameof(LibraryItemInstanceStatus.InShelf) && // In-shelf status
                    li.LibraryItemId == existingEntity.LibraryItemId &&
                    li.LibraryItemInstanceId != existingEntity.LibraryItemInstanceId); // Exclude update instance
                var inShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countSpec);
                if (pendingRequestUnits > inShelfUnits)
                {
                    // Msg: Unable to put the items out of shelf as the number of items on
                    // the shelf cannot be less than the number of borrowing requests
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0025,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0025));
                }
                
                // Reduce current available value
                if(currentInventory.AvailableUnits > 0) currentInventory.AvailableUnits -= 1;
            }
            else if (enumStatus == LibraryItemInstanceStatus.InShelf)
            {
                // Count total pending reserve
                var pendingReservedUnits = currentInventory.ReservedUnits;
                // Count total out-of-shelf units
                var countSpec = new BaseSpecification<LibraryItemInstance>(li =>
                    li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf) && // In-shelf status
                    li.LibraryItemId == existingEntity.LibraryItemId &&
                    li.LibraryItemInstanceId != existingEntity.LibraryItemInstanceId); // Exclude update instance
                var outOfShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countSpec);
                if (pendingReservedUnits > outOfShelfUnits)
                {
                    var warningMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0024);
                    // Msg: There are currently {0} pending reservations. Please confirm before placing the items on the shelf
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0024,
                        StringUtils.Format(warningMsg, pendingReservedUnits.ToString()));
                }
                
                // Increase current available value
                currentInventory.AvailableUnits += 1;
            }

            // Progress update when all require passed
            await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(existingEntity);

            // Progress update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Progress update can borrow status of library item
            await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                id: existingEntity.LibraryItemId,
                canBorrow: currentInventory.AvailableUnits > 0);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
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
            throw;
        }
    }

    public async Task<IServiceResult> GenerateBarcodeRangeAsync(int categoryId, int totalItem, int? skipItem = 0)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Validate length
            if (totalItem <= 0)
            {
                // Data not found or empty
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004);
                var customMsg = isEng ? "Total item must greater than 0" : "Tổng số lượng phải lớn hơn 0";
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, $"{msg}.{customMsg}");
            }
            
            // Check exist category
            var categoryDto = (await _categoryService.GetByIdAsync(categoryId)).Data as CategoryDto;
            if (categoryDto == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                    ? "category to process auto filled item's barcode" 
                    : "phân loại để tạo số đăng ký cá biệt cho tài liệu"));
            }
            
            // Retrieve default maximum instance barcode length
            var defaultBarcodeLength = _appSettings.InstanceBarcodeNumLength;
            // Extract category prefix 
            var prefix = categoryDto.Prefix;
            // Initialize barcode range fields
            string barcodeRangeFrom;
            string barcodeRangeTo;
            // Initialize start index
            int startIndex = 0;
            
            // Retrieve all item's barcode
            // Build spec
            var instanceSpec = new BaseSpecification<LibraryItemInstance>(li => 
                li.LibraryItem.CategoryId == categoryId); // All instance's item that match with specific category id
            var existingBarcodes = (await _unitOfWork.Repository<LibraryItemInstance, int>()
                    .GetAllWithSpecAndSelectorAsync(instanceSpec, selector: li => li.Barcode)).ToList(); // Convert result to array
            if (existingBarcodes.Any()) // At least one found
            {
                // Try to retrieve latest barcode in warehouse tracking detail 
                var latestBarcode =
                    (await _whTrackingDetailService.Value.GetLatestBarcodeByCategoryIdAsync(categoryId: categoryId)).Data as string;
                // Append latest warehouse tracking detail barcode (if exist)
                if(!string.IsNullOrEmpty(latestBarcode)) existingBarcodes.Add(latestBarcode);
                
                // Convert existing barcodes to list of int to retrieve the highest one
                startIndex = existingBarcodes
                    // Iterate each barcode, converting them to number
                    .Select(barcode => StringUtils.ExtractNumber(input: barcode, prefix: prefix, length: defaultBarcodeLength))
                    // Order by number descending
                    .OrderByDescending(num => num)
                    .FirstOrDefault();
                
                // Lengthen the start index when exist skip item
                if (skipItem != null && int.TryParse(skipItem.ToString(), out var validSkipVal))
                {
                    startIndex += validSkipVal;
                }
                
                // Add barcode range
                barcodeRangeFrom = StringUtils.AutoCompleteBarcode(prefix: prefix, length: defaultBarcodeLength, number: startIndex + 1);
                barcodeRangeTo = StringUtils.AutoCompleteBarcode(prefix: prefix, length: defaultBarcodeLength, number: startIndex + totalItem);
            }
            else // Not found any
            {
                // Lengthen the start index when exist skip item
                if (skipItem != null && int.TryParse(skipItem.ToString(), out var validSkipVal))
                {
                    startIndex += validSkipVal;
                }
                
                var lowerBoundary = startIndex > 0 ? startIndex + 1 : 1;
                var upperBoundary = startIndex > 0 ? startIndex + totalItem : totalItem;
                
                // Add default barcode range
                barcodeRangeFrom = StringUtils.AutoCompleteBarcode(prefix: prefix, length: defaultBarcodeLength, number: lowerBoundary);
                barcodeRangeTo = StringUtils.AutoCompleteBarcode(prefix: prefix, length: defaultBarcodeLength, number: upperBoundary);
            }
            
            // Check whether upper boundary exceed than instance barcode length
            var upperBoundNum = barcodeRangeTo.Length - prefix.Length;
            if (upperBoundNum > defaultBarcodeLength)
            {
                // Msg: The number of instance item is exceeding than default config threshold. Please modify system configuration to continue
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0015,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0015));   
            }
            
            // Extract current lower boundary num
            var currLowerBoundNum = StringUtils.ExtractNumber(input: barcodeRangeFrom, prefix: prefix, length: defaultBarcodeLength);
            // Extract current upper boundary num
            var currUpperBoundNum = StringUtils.ExtractNumber(input: barcodeRangeTo, prefix: prefix, length: defaultBarcodeLength);
            // Generate list of barcode 
            var barcodeList = StringUtils.AutoCompleteBarcode(
                prefix: prefix,
                length: defaultBarcodeLength,
                min: currLowerBoundNum,
                max: currUpperBoundNum);
            
            // Always return success (if any category found)
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                new GenerateBarcodeRangeResultDto()
                {
                    BarcodeRangeFrom = barcodeRangeFrom,
                    BarcodeRangeTo = barcodeRangeTo,
                    Barcodes = barcodeList
                });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when generate barcode range with specific length and category");
        }
    }
    
    public async Task<IServiceResult> GetByBarcodeAsync(string barcode)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build specification
            var baseSpec = new BaseSpecification<LibraryItemInstance>(li => Equals(li.Barcode, barcode));
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, selector: li => new LibraryItemInstance()
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
                    LibraryItem = new LibraryItem()
                    {
                        LibraryItemId = li.LibraryItem.LibraryItemId,
                        Title = li.LibraryItem.Title,
                        SubTitle = li.LibraryItem.SubTitle,
                        Responsibility = li.LibraryItem.Responsibility,
                        Edition = li.LibraryItem.Edition,
                        EditionNumber = li.LibraryItem.EditionNumber,
                        Language = li.LibraryItem.Language,
                        OriginLanguage = li.LibraryItem.OriginLanguage,
                        Summary = li.LibraryItem.Summary,
                        CoverImage = li.LibraryItem.CoverImage,
                        PublicationYear = li.LibraryItem.PublicationYear,
                        Publisher = li.LibraryItem.Publisher,
                        PublicationPlace = li.LibraryItem.PublicationPlace,
                        ClassificationNumber = li.LibraryItem.ClassificationNumber,
                        CutterNumber = li.LibraryItem.CutterNumber,
                        Isbn = li.LibraryItem.Isbn,
                        Ean = li.LibraryItem.Ean,
                        EstimatedPrice = li.LibraryItem.EstimatedPrice,
                        PageCount = li.LibraryItem.PageCount,
                        PhysicalDetails = li.LibraryItem.PhysicalDetails,
                        Dimensions = li.LibraryItem.Dimensions,
                        AccompanyingMaterial = li.LibraryItem.AccompanyingMaterial,
                        Genres = li.LibraryItem.Genres,
                        GeneralNote = li.LibraryItem.GeneralNote,
                        BibliographicalNote = li.LibraryItem.BibliographicalNote,
                        TopicalTerms = li.LibraryItem.TopicalTerms,
                        AdditionalAuthors = li.LibraryItem.AdditionalAuthors,
                        CategoryId = li.LibraryItem.CategoryId,
                        ShelfId = li.LibraryItem.ShelfId,
                        GroupId = li.LibraryItem.GroupId,
                        Status = li.LibraryItem.Status,
                        IsDeleted = li.LibraryItem.IsDeleted,
                        IsTrained = li.LibraryItem.IsTrained,
                        CanBorrow = li.LibraryItem.CanBorrow,
                        TrainedAt = li.LibraryItem.TrainedAt,
                        CreatedAt = li.LibraryItem.CreatedAt,
                        UpdatedAt = li.LibraryItem.UpdatedAt,
                        UpdatedBy = li.LibraryItem.UpdatedBy,
                        CreatedBy = li.LibraryItem.CreatedBy,
                        // References
                        Category = li.LibraryItem.Category,
                        Shelf = li.LibraryItem.Shelf,
                        LibraryItemInventory = li.LibraryItem.LibraryItemInventory,
                        LibraryItemAuthors = li.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                        {
                            LibraryItemAuthorId = ba.LibraryItemAuthorId,
                            LibraryItemId = ba.LibraryItemId,
                            AuthorId = ba.AuthorId,
                            Author = ba.Author
                        }).ToList()
                    },
                    LibraryItemConditionHistories = li.LibraryItemConditionHistories.Select(c => new LibraryItemConditionHistory()
                    {
                        ConditionHistoryId = c.ConditionHistoryId,
                        LibraryItemInstanceId = c.LibraryItemInstanceId,
                        Condition = c.Condition,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        CreatedBy = c.CreatedBy,
                        UpdatedBy = c.UpdatedBy,
                    }).OrderByDescending(x => x.CreatedAt).ToList()
                });
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item instance" : "tài liệu"));
            }
            
            // Convert to dto
            var dto = _mapper.Map<LibraryItemInstanceDto>(existingEntity);
            // Get data success
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                // Convert to get instance by barcode result dto
                dto.ToGetByBarcodeResultDto());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get library item instance by barcode");
        }
    }

    public async Task<IServiceResult> GetByBarcodeToConfirmUpdateShelfAsync(string barcode)
    {
        try
        {   
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(li => Equals(li.Barcode, barcode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item to process update shelf status" 
                        : "tài liệu để cập nhật trạng thái"));
            }
            else if (existingEntity.LibraryItem.ShelfId == null)
            {
                // Msg: The item's status cannot be updated because it has not been shelved yet
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0019,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0019));
            }
            
            // Error msg: The item's shelf status cannot be updated as {0}
            var constraintMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0020); 
            // Check constraint
            if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Borrowed))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently 'borrowed'" 
                        : "trạng thái của tài liệu hiện đang mượn"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Reserved))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently 'reserved'" 
                        : "trạng thái của tài liệu hiện đang được đặt trước"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Lost))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently 'lost'" 
                        : "trạng thái của tài liệu hiện đang bị mất"));
            }
            
            // Map to instance dto
            var instanceDto = _mapper.Map<LibraryItemInstanceDto>(existingEntity);
            // Msg: Get data successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                // Convert to library item instance detail
                instanceDto.ToItemInstanceDetailDto());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get library item instance by barcode to confirm update shelf");
        }
    }
    
    public async Task<IServiceResult> CheckExistBarcodeAsync(string barcode)
    {
        try
        {
            var isExistBarcode = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .AnyAsync(li => Equals(li.Barcode.ToLower(), barcode.ToLower()));
            if (isExistBarcode)
            {
                // Msg: Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
            }
            
            // Msg: Fail to get data
            return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check exist barcode");
        }
    }
    
    public async Task<IServiceResult> AddRangeToLibraryItemAsync(int libraryItemId,
        List<LibraryItemInstanceDto> itemInstances)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            // Build spec
            var libItemSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == libraryItemId);
            // Apply include
            libItemSpec.ApplyInclude(q => q.Include(li => li.Category));
            // Retrieve item with spec
            var libItemDto = (await _libItemService.GetWithSpecAsync(libItemSpec)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item to process add range instance" 
                        : "tài liệu để thêm nhiều bản sao"));
            }
            
            // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            var uniqueList = new HashSet<string>();
            // Check exist code
            for (int i = 0; i < itemInstances.Count; i++)
            {
                // Validate condition status (check for first only)
                var conditionHistories = itemInstances[i].LibraryItemConditionHistories.ToList();
                if (conditionHistories.Count > 1)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Fail0001, isEng
                        ? "Not allow to add multiple condition histories"
                        : "Không được thêm nhiều trạng thái bản sao ban đầu");
                }
                else
                {
                    // Check exist condition
                    var isConditionExist = (await _conditionService.AnyAsync(c =>
                        c.ConditionId == conditionHistories[0].ConditionId)).Data is true;
                    if (!isConditionExist)
                    {
                        // Add error 
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                            key: $"libraryItemInstances[{i}].conditionId",
                            msg: isEng ? "Condition not exist" : "Trạng thái không tồn tại");
                    }
                }

                if (uniqueList.Add(itemInstances[i].Barcode)) // Valid barcode
                {
                    // Check exist code in DB
                    var isExist = await _unitOfWork.Repository<LibraryItemInstance, int>().AnyAsync(x =>
                        x.Barcode == itemInstances[i].Barcode);
                    if (isExist) // already exist
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0005);
                        // Add error 
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                            key: $"libraryItemInstances[{i}].barcode",
                            msg: StringUtils.Format(errMsg, $"'{itemInstances[i].Barcode}'"));
                    }
                    
                    // Try to validate with category prefix
                    var isValidBarcode =
                        StringUtils.IsValidBarcodeWithPrefix(itemInstances[i].Barcode, libItemDto.Category.Prefix);
                    if (!isValidBarcode)
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
                        // Add errors
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                            key: $"libraryItemInstances[{i}].barcode",
                            msg: StringUtils.Format(errMsg, $"'{libItemDto.Category.Prefix}'"));
                    }
                        
                    // Try to validate barcode length
                    var barcodeNumLength = itemInstances[i].Barcode.Length - libItemDto.Category.Prefix.Length; 
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
                else
                {
                    // Add error 
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                        key: $"libraryItemInstances[{i}].barcode",
                        msg: isEng
                            ? $"Barcode '{itemInstances[i].Barcode}' is duplicated"
                            : $"Số đăng ký cá biệt '{itemInstances[i].Barcode}' đã bị trùng");
                }
            }

            // Check if any error invoke
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }

            // Check exist library item
            var itemEntity = (await _libItemService.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
            if (itemEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item" : "bản sao"));
            }

            var toAddItemInstances = new List<LibraryItemInstance>();
            // Process add new item instance
            itemInstances.ForEach(bec =>
            {
                toAddItemInstances.Add(new()
                {
                    // Assign to specific library item
                    LibraryItemId = itemEntity.LibraryItemId,
                    // Assign copy barcode
                    Barcode = bec.Barcode,
                    // Default status
                    Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
                    // Boolean 
                    IsDeleted = false,
                    IsCirculated = false,
                    // Condition histories
                    LibraryItemConditionHistories =
                        _mapper.Map<List<LibraryItemConditionHistory>>(bec.LibraryItemConditionHistories)
                });
            });

            // Add range 
            await _unitOfWork.Repository<LibraryItemInstance, int>().AddRangeAsync(toAddItemInstances);

            // Update inventory total
            // Get inventory by library item id
            var getInventoryRes = await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    x => x.LibraryItemId == libraryItemId), tracked: false);
            if (getInventoryRes.Data is LibraryItemInventoryDto inventoryDto) // Get data success
            {
                // Set relations to null
                inventoryDto.LibraryItem = null!;
                // Update total
                inventoryDto.TotalUnits += toAddItemInstances.Count;

                // Update without save
                await _inventoryService.UpdateWithoutSaveChangesAsync(inventoryDto);
            }

            // Save DB
            var rowEffected = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (rowEffected == 0)
            {
                // Fail to save
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
            }

            // Save successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add item instance to library item");
        }
    }

    public async Task<IServiceResult> AddRangeBarcodeWithoutSaveChangesAsync(string isbn,
        int conditionId, string barcodeRangeFrom, string barcodeRangeTo)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item
            // Build spec
            var libItemSpec = new BaseSpecification<LibraryItem>(li => Equals(li.Isbn, isbn));
            // Apply include
            libItemSpec.ApplyInclude(q => q.Include(li => li.Category));
            // Retrieve item with spec
            var libItemDto = (await _libItemService.GetWithSpecAsync(libItemSpec)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                // Log
                _logger.Error("Not found library item to process add range barcode without save changes");
                // Mark as fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), data: 0);
            }
            
            // Check exist condition
            var isConditionExist = (await _conditionService.AnyAsync(c => c.ConditionId == conditionId)).Data is true;
            if (!isConditionExist)
            {
                // Log
                _logger.Error("Not found library item condition to process add range barcode without save changes");
                // Mark as fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), data: 0);
            }
            
            // Extract category prefix
            var prefix = libItemDto.Category.Prefix;
            // Extract current lower boundary num
            var currLowerBoundNum = StringUtils.ExtractNumber(input: barcodeRangeFrom, prefix: prefix, length: _appSettings.InstanceBarcodeNumLength);
            // Extract current upper boundary num
            var currUpperBoundNum = StringUtils.ExtractNumber(input: barcodeRangeTo, prefix: prefix, length: _appSettings.InstanceBarcodeNumLength);
            // Generate list of barcode 
            var barcodeList = StringUtils.AutoCompleteBarcode(
                prefix: prefix,
                length: _appSettings.InstanceBarcodeNumLength,
                min: currLowerBoundNum,
                max: currUpperBoundNum);
            
            // Initialize list of library item instance
            var itemInstances = new List<LibraryItemInstanceDto>();
            // Iterate each barcode generated to add library item instance and its default condition history
            foreach (var barcode in barcodeList)
            {
                itemInstances.Add(new ()
                {
                    Barcode = barcode,
                    Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
                    LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
                    {
                        new()
                        {
                            ConditionId = conditionId
                        }
                    }
                });
            }
            
            // Check exist library item
            var itemEntity = (await _libItemService.GetByIdAsync(libItemDto.LibraryItemId)).Data as LibraryItemDto;
            if (itemEntity == null)
            {
                // Log
                _logger.Error("Not found library item to process add range barcode without save changes");
                // Mark as fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), data: 0);
            }

            var toAddItemInstances = new List<LibraryItemInstance>();
            // Process add new item instance
            itemInstances.ForEach(bec =>
            {
                toAddItemInstances.Add(new()
                {
                    // Assign to specific library item
                    LibraryItemId = itemEntity.LibraryItemId,
                    // Assign copy barcode
                    Barcode = bec.Barcode,
                    // Default status
                    Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
                    // Boolean 
                    IsDeleted = false,
                    IsCirculated = false,
                    // Condition histories
                    LibraryItemConditionHistories =
                        _mapper.Map<List<LibraryItemConditionHistory>>(bec.LibraryItemConditionHistories)
                });
            });

            // Add range 
            await _unitOfWork.Repository<LibraryItemInstance, int>().AddRangeAsync(toAddItemInstances);

            // Update inventory total
            // Get inventory by library item id
            var getInventoryRes = await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    x => x.LibraryItemId == itemEntity.LibraryItemId), tracked: false);
            if (getInventoryRes.Data is LibraryItemInventoryDto inventoryDto) // Get data success
            {
                // Set relations to null
                inventoryDto.LibraryItem = null!;
                // Update total
                inventoryDto.TotalUnits += toAddItemInstances.Count;

                // Update without save
                await _inventoryService.UpdateWithoutSaveChangesAsync(inventoryDto);
            }

            // Mark as success without save change
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), data: itemEntity.LibraryItemId);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add item instance to library item");
        }
    }
    
    public async Task<IServiceResult> UpdateRangeAsync(int libraryItemId,
        List<LibraryItemInstanceDto> itemInstanceDtos)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist to update items
            if (!itemInstanceDtos.Any())
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "any item instance match" : "các bản sao cần sửa"));
            }
            
            // Try to retrieve inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id 
                    x => x.LibraryItemId == libraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred 
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }
            
            // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            // Initialize hash set to check unique barcode
            var uniqueCodeSet = new HashSet<string>();
            // Initialize counter
            var totalUpdateInShelf = 0;
            var totalUpdateOutOfShelf = 0;
            // Iterate each item instances to check for update properties 
            for (int i = 0; i < itemInstanceDtos.Count; i++)
            {
                var iInstanceDto = itemInstanceDtos[i];
                
                // Build spec base
                var itemInstanceSpec = new BaseSpecification<LibraryItemInstance>(
                    x => 
                        x.LibraryItemInstanceId == iInstanceDto.LibraryItemInstanceId && // Specific item instance 
                        x.LibraryItemId == libraryItemId); // Must in the same library item
                itemInstanceSpec.ApplyInclude(q => q
                    // Include library item
                    .Include(bec => bec.LibraryItem)
                    // Include category
                    .ThenInclude(li => li.Category)
                );
                // Get library item instance by id and include constraints
                var itemInstanceEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                    .GetWithSpecAsync(itemInstanceSpec);
                if (itemInstanceEntity == null || itemInstanceEntity.IsDeleted) // not exist
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    
                    // Error key
                    var key = $"libraryItemInstances.[{i}]";
                    // Error msg
                    var msg = StringUtils.Format(errMsg, isEng ? "item instance" : "bản sao");
                    // Add error dic 
                    customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                }
                else
                {
                    // Mark as no data effected
                    var isUpdated = false;
                    // Validate status type
                    Enum.TryParse(typeof(LibraryItemInstanceStatus), iInstanceDto.Status, out var validStatus);
                    if (validStatus == null)
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                            StringUtils.Format(errMsg, isEng ? "Because status is not valid" : "Do trạng thái không hợp lệ"));
                    }
                    
                    // Parse into enum
                    var toUpdateStatus = (LibraryItemInstanceStatus)validStatus;
                    // Check if status change
                    if (!Equals(itemInstanceEntity.Status, toUpdateStatus.ToString())) // Change detected
                    {
                        // Msg: Cannot update item instance status as {0}
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0022);
                        
                        // Do not allow to update BORROWED/RESERVED status
                        // With RESERVED status of item instance, it will change automatically when 
                        // someone return their borrowed book and assigned that book to others, who are in reservation queue
                        if (toUpdateStatus == LibraryItemInstanceStatus.Borrowed ||
                            toUpdateStatus == LibraryItemInstanceStatus.Reserved ||
                            toUpdateStatus == LibraryItemInstanceStatus.Lost)
                        {
                            // Error key
                            var key = $"libraryItemInstances.[{i}].status";
                            // Add error dic 
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
                                key: key, 
                                msg: StringUtils.Format(errMsg, isEng 
                                     ? "selected status is invalid" 
                                     : "trạng thái thay đổi không hợp lệ"));
                        }
                        else if (toUpdateStatus == LibraryItemInstanceStatus.OutOfShelf) totalUpdateOutOfShelf++;
                        else if (toUpdateStatus == LibraryItemInstanceStatus.InShelf)
                        {
                            // Required exist shelf location in library item for update to in-shelf status
                            if (itemInstanceEntity.LibraryItem.ShelfId == null || itemInstanceEntity.LibraryItem.ShelfId == 0)
                            {
                                errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                                // Required shelf location
                                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                                    StringUtils.Format(errMsg, isEng
                                        ? "Shelf location not found"
                                        : "Không tìm thấy vị trí kệ cho sách"));
                            }
                            
                            // Increase total update in-shelf
                            totalUpdateInShelf++;
                        }
                        
                        var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(bRec => 
                                    bRec.BorrowRecordDetails.Any(brd => 
                                        brd.LibraryItemInstanceId == itemInstanceEntity.LibraryItemInstanceId && // Exist in any borrow record details
                                        brd.Status != BorrowRecordStatus.Returned)) // Exclude elements with returned status
                            ).Data is true; // Convert object to boolean 
                        
                        if (hasBorrowRecordConstraint) // Has any constraint 
                        {
                            // Error key
                            var key = $"libraryItemInstances.[{i}]";
                            // Error msg
                            var msg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008);
                            // Add error dic
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                        }
                        
                        // Process update status
                        itemInstanceEntity.Status = iInstanceDto.Status;
                        // Change to updated
                        isUpdated = true;
                        
                        // Progress update inventory
                        // Check for typeof status update
                        if (toUpdateStatus == LibraryItemInstanceStatus.OutOfShelf)
                        {
                            // Reduce current available value
                            if(currentInventory.AvailableUnits > 0) currentInventory.AvailableUnits -= 1;
                        }
                        else if (toUpdateStatus == LibraryItemInstanceStatus.InShelf)
                        {
                            // Increase current available value
                            currentInventory.AvailableUnits += 1;
                        }
                    }
                    
                    // Check if barcode change
                    if (!Equals(itemInstanceEntity.Barcode, iInstanceDto.Barcode)) // Change detected
                    {
                        // Try to add barcode to hash set to remain uniqueness among barcode within update items
                        if (!uniqueCodeSet.Add(iInstanceDto.Barcode)) // Fail to add, due to duplicate
                        {
                            // Error key
                            var key = $"libraryItemInstances.[{i}].barcode";
                            // Error msg
                            var msg = isEng
                                ? $"Barcode '{iInstanceDto.Barcode}' is duplicated"
                                : $"Số đăng ký cá biệt '{iInstanceDto.Barcode}' đã bị trùng";
                            // Add error dic
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                        }
                        
                        // Check exist barcode
                        var isBarcodeExist = await _unitOfWork.Repository<LibraryItemInstance, int>()
                            .AnyAsync(li => 
                                    Equals(li.Barcode.ToLower(), iInstanceDto.Barcode.ToLower()) && // Same barcode format + suffix
                                    !Equals(li.LibraryItemInstanceId, iInstanceDto.LibraryItemInstanceId) // Check with other item instances
                            );
                        if (isBarcodeExist)
                        {
                            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0005);
                            
                            // Error key
                            var key = $"libraryItemInstances.[{i}].barcode";
                            // Error msg 
                            var msg = StringUtils.Format(errMsg, $"'{iInstanceDto.Barcode}'");
                            // Add error dic
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                        }
                        else // Check valid barcode prefix
                        {
                            // Retrieve category prefix
                            var catePrefix = itemInstanceEntity.LibraryItem.Category.Prefix;
                            // Check whether new barcode match prefix
                            var isMatched = StringUtils.IsValidBarcodeWithPrefix(iInstanceDto.Barcode, catePrefix);
                            if (!isMatched) // Not matched
                            {
                                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
                                
                                // Error key
                                var key = $"libraryItemInstances.[{i}].barcode";
                                // Error msg
                                var msg = StringUtils.Format(errMsg, $"'{catePrefix}'");
                                // Add error dic
                                customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                            }
                        }
                
                        // Progress update barcode
                        itemInstanceEntity.Barcode = iInstanceDto.Barcode;
                        // Change to updated
                        isUpdated = true;
                    }
                    
                    // Progress update
                    if(isUpdated) await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(itemInstanceEntity);
                }
            }
            
            // Check if any error invoke
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }
            
            // Extract all instance ids
            var instanceIds = itemInstanceDtos.Select(i => i.LibraryItemInstanceId);
            
            // Validate units after update
            // Count total pending reserve
            var pendingReservedUnits = currentInventory.ReservedUnits;
            // Count total out-of-shelf units
            var countOutSpec = new BaseSpecification<LibraryItemInstance>(li =>
                li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf) && // In-shelf status
                li.LibraryItemId == libraryItemId &&
                !instanceIds.Contains(li.LibraryItemInstanceId)); // Exclude update instance
            var outOfShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countOutSpec);
            if (pendingReservedUnits > outOfShelfUnits && totalUpdateInShelf > 0)
            {
                var warningMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0024);
                // Msg: There are currently {0} pending reservations. Please confirm before placing the items on the shelf
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0024,
                    StringUtils.Format(warningMsg, pendingReservedUnits.ToString()));
            }
            
            // Count total pending request
            var pendingRequestUnits = currentInventory.RequestUnits;
            // Count total in-shelf units
            var countInSpec = new BaseSpecification<LibraryItemInstance>(li =>
                li.Status == nameof(LibraryItemInstanceStatus.InShelf) && // In-shelf status
                li.LibraryItemId == libraryItemId &&
                !instanceIds.Contains(li.LibraryItemInstanceId)); // Exclude update instances
            var inShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countInSpec);
            if (pendingRequestUnits > inShelfUnits && totalUpdateOutOfShelf > 0)
            {
                // Msg: Unable to put the items out of shelf as the number of items on
                // the shelf cannot be less than the number of borrowing requests
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0025,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0025));
            }
            
            // Progress update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
            
            // Progress update can borrow status of library item
            await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                id: libraryItemId,
                canBorrow: currentInventory.AvailableUnits > 0);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
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
            throw new Exception("Error invoke when process update range library item copy");
        }
    }

    public async Task<IServiceResult> UpdateInShelfAsync(string barcode)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(li => Equals(li.Barcode, barcode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item to process update shelf status" 
                        : "tài liệu để cập nhật trạng thái"));
            }
            else if (existingEntity.LibraryItem.ShelfId == null)
            {
                // Msg: The item's status cannot be updated because it has not been shelved yet
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0019,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0019));
            }
         
            // Error msg: Cannot update item's shelf status to 'in-shelf' as {0}
            var constraintMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0020); 
            // Check constraint
            if (existingEntity.Status == nameof(LibraryItemInstanceStatus.InShelf))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item has been shelved" 
                        : "tài liệu ở tình trạng đã được xếp lên kệ"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Borrowed))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently borrowed" 
                        : "trạng thái của tài liệu hiện đang mượn"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Reserved))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently reserved" 
                        : "trạng thái của tài liệu hiện đang được đặt trước"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Lost))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0020,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently lost" 
                        : "trạng thái của tài liệu hiện đang bị mất"));
            }
            
            // Change status
            existingEntity.Status = nameof(LibraryItemInstanceStatus.InShelf);
            
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<LibraryItemInstance, int>().HasChanges(existingEntity))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0001), true);
            }
            
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == existingEntity.LibraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }
            // Increase current available value
            currentInventory.AvailableUnits += 1;
            
            // Progress update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
            
            // Progress update can borrow status of library item
            await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                id: existingEntity.LibraryItemId,
                canBorrow: currentInventory.AvailableUnits > 0);
            
            // Process update
            await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Item has been shelved successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0001));
            }
            
            // Msg: Shelving the item was unsuccessfully
            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update range library instance in shelf");
        }
    }

    public async Task<IServiceResult> UpdateOutOfShelfAsync(string barcode)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(li => Equals(li.Barcode, barcode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
                    .ThenInclude(li => li.LibraryItemInventory!)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item to process update shelf status" 
                        : "tài liệu để cập nhật trạng thái"));
            }
            else if (existingEntity.LibraryItem.ShelfId == null)
            {
                // Msg: The item's status cannot be updated because it has not been shelved yet
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0019,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0019));
            }
         
            // Error msg: Cannot update item's shelf status to 'out-of-shelf' as {0}
            var constraintMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0021); 
            // Check constraint
            if (existingEntity.Status == nameof(LibraryItemInstanceStatus.OutOfShelf))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item has been shelved" 
                        : "tài liệu đang không nằm trên kệ"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Borrowed))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently borrowed" 
                        : "trạng thái của tài liệu hiện đang mượn"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Reserved))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently reserved" 
                        : "trạng thái của tài liệu hiện đang được đặt trước"));
            }
            else if (existingEntity.Status == nameof(LibraryItemInstanceStatus.Lost))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                    StringUtils.Format(constraintMsg, isEng 
                        ? "the item's status is currently lost" 
                        : "trạng thái của tài liệu hiện đang bị mất"));
            }
            
            // Check remain pending requests
            var pendingRequestUnits = existingEntity.LibraryItem.LibraryItemInventory?.RequestUnits ?? 0;
            // Count total in-shelf units
            var countSpec = new BaseSpecification<LibraryItemInstance>(li =>
                li.Status == nameof(LibraryItemInstanceStatus.InShelf) && // In-shelf status
                li.LibraryItemId == existingEntity.LibraryItemId &&
                li.LibraryItemInstanceId != existingEntity.LibraryItemInstanceId); // Exclude update instance
            var inShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countSpec);
            if (pendingRequestUnits > inShelfUnits)
            {
                // Msg: Unable to put the items out of shelf as the number of items on
                // the shelf cannot be less than the number of borrowing requests
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0025,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0025));
            }
            
            // Change status
            existingEntity.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
            
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<LibraryItemInstance, int>().HasChanges(existingEntity))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0003), true);
            }

            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == existingEntity.LibraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }
            // Reduce current available value
            if(currentInventory.AvailableUnits > 0) currentInventory.AvailableUnits -= 1;
            
            // Progress update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Progress update can borrow status of library item
            await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                id: existingEntity.LibraryItemId,
                canBorrow: currentInventory.AvailableUnits > 0);
            
            // Process update
            await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Item has been shelved successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0003));
            }
            
            // Msg: Unshelving the item was unsuccessfully
            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update range library instance in shelf");
        }
    }
    
    public async Task<IServiceResult> UpdateRangeInShelfAsync(List<string> barcodes)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(
                li => barcodes.Contains(li.Barcode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
                    .ThenInclude(li => li.LibraryItemInventory!)
            );
            // Retrieve all with spec
            var entities = (await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec)).ToList();
            if (!entities.Any())
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "any items to process shelving"
                        : "tài liệu để tiến hành xếp kệ"));
            }

            // Group all instance by library item id
            var groupedEntities = entities
                .GroupBy(e => e.LibraryItemId)
                .Select(e => new { Key = e.Key, Value = e.ToList() });
            
            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Iterate each grouped item instances
            foreach (var gr in groupedEntities)
            {
                var libraryItemId = gr.Key;
                var instances = gr.Value;
                
                // Try to retrieve inventory data
                var currentInventory = (await _inventoryService.GetWithSpecAsync(
                    new BaseSpecification<LibraryItemInventory>(
                        // With specific library item id 
                        x => x.LibraryItemId == libraryItemId))).Data as LibraryItemInventoryDto;
                if (currentInventory == null) // Not found inventory
                {
                    // An error occurred 
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                }
                
                // Iterate item instances
                for (int i = 0; i < instances.Count; ++i)
                {
                    var instance = instances[i];
                    
                    // Error msg: The item's shelf status cannot be updated as {0}
                    var constraintMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0020);
                    // Check constraint
                    if (instance.Status == nameof(LibraryItemInstanceStatus.InShelf))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs, 
                            key: $"barcodes[{i}]", 
                            msg: StringUtils.Format(constraintMsg, isEng
                                ? "the item has been shelved"
                                : "tài liệu ở tình trạng đã được xếp lên kệ"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Borrowed))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs, 
                            key: $"barcodes[{i}]", 
                            msg: StringUtils.Format(constraintMsg, isEng
                                ? "the item's status is currently borrowed"
                                : "trạng thái của tài liệu hiện đang mượn"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Reserved))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs, 
                            key: $"barcodes[{i}]", 
                            msg: StringUtils.Format(constraintMsg, isEng
                                ? "the item's status is currently reserved"
                                : "trạng thái của tài liệu hiện đang được đặt trước"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Lost))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs, 
                            key: $"barcodes[{i}]", 
                            msg: StringUtils.Format(constraintMsg, isEng
                                ? "the item's status is currently lost"
                                : "trạng thái của tài liệu hiện đang bị mất"));
                    }

                    // Increase current available value
                    currentInventory.AvailableUnits += 1;
                    // Change status
                    instance.Status = nameof(LibraryItemInstanceStatus.InShelf);
                    
                    // Process update
                    await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(instance);
                }
                
                // Progress update inventory
                await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
                
                // Progress update can borrow status of library item
                await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                    id: libraryItemId,
                    canBorrow: currentInventory.AvailableUnits > 0);
            }


            // Check whether invoke any error
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid Data", customErrs);
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Total {0} item have been shelved successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0002,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0002),
                        entities.Count.ToString()));
            }

            // Msg: Shelving the item was unsuccessfully
            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0003));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update range item instance status");
        }
    }
    
    public async Task<IServiceResult> UpdateRangeOutOfShelfAsync(List<string> barcodes)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(
                li => barcodes.Contains(li.Barcode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
            );
            // Retrieve all with spec
            var entities = (await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec)).ToList();
            if (!entities.Any())
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "any items to process shelving"
                        : "tài liệu để tiến hành xếp kệ"));
            }

            // Group all instance by library item id
            var groupedEntities = entities
                .GroupBy(e => e.LibraryItemId)
                .Select(e => new { Key = e.Key, Value = e.ToList() });
            
            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Iterate each grouped item instances
            foreach (var gr in groupedEntities)
            {
                var libraryItemId = gr.Key;
                var instances = gr.Value;
                
                // Try to retrieve inventory data
                var currentInventory = (await _inventoryService.GetWithSpecAsync(
                    new BaseSpecification<LibraryItemInventory>(
                        // With specific library item id 
                        x => x.LibraryItemId == libraryItemId))).Data as LibraryItemInventoryDto;
                if (currentInventory == null) // Not found inventory
                {
                    // An error occurred 
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                }
                
                // Iterate item instances
                for (int i = 0; i < instances.Count; ++i)
                {
                    var instance = instances[i];
                    
                    // Error msg: Cannot update item's shelf status to 'out-of-shelf' as {0}
                    var constraintMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0021); 
                    // Check constraint
                    if (instance.Status == nameof(LibraryItemInstanceStatus.OutOfShelf))
                    {
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                            StringUtils.Format(constraintMsg, isEng 
                                ? "the item has been shelved" 
                                : "tài liệu đang không nằm trên kệ"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Borrowed))
                    {
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                            StringUtils.Format(constraintMsg, isEng 
                                ? "the item's status is currently borrowed" 
                                : "trạng thái của tài liệu hiện đang mượn"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Reserved))
                    {
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                            StringUtils.Format(constraintMsg, isEng 
                                ? "the item's status is currently 'reserved'" 
                                : "trạng thái của tài liệu hiện đang được đặt trước"));
                    }
                    else if (instance.Status == nameof(LibraryItemInstanceStatus.Lost))
                    {
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0021,
                            StringUtils.Format(constraintMsg, isEng 
                                ? "the item's status is currently lost" 
                                : "trạng thái của tài liệu hiện đang bị mất"));
                    }

                    // Increase current available value
                    if (currentInventory.AvailableUnits > 0) currentInventory.AvailableUnits -= 1;
                    // Change status
                    instance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                    
                    // Process update
                    await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(instance);
                }   
                
                // Count total pending request
                var pendingRequestUnits = currentInventory.RequestUnits;
                // Extract all instance ids
                var instanceIds = instances.Select(i => i.LibraryItemInstanceId).ToList();
                // Count total in-shelf units
                var countSpec = new BaseSpecification<LibraryItemInstance>(li =>
                    li.Status == nameof(LibraryItemInstanceStatus.InShelf) && // In-shelf status
                    li.LibraryItemId == libraryItemId &&
                    !instanceIds.Contains(li.LibraryItemInstanceId)); // Exclude update instances
                var inShelfUnits = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(countSpec);
                if (pendingRequestUnits > inShelfUnits)
                {
                    // Msg: Unable to put out of shelf for item {0} as the number of items on
                    // shelf cannot be less than the number of borrowing requests
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0026);
                    var itemName = instances.First().LibraryItem.Title;
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0026,
                        StringUtils.Format(errMsg, $"'{itemName}'"));
                }
                
                // Progress update inventory
                await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
                
                // Progress update can borrow status of library item
                await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                    id: libraryItemId,
                    canBorrow: currentInventory.AvailableUnits > 0);
            }
            
            // Check whether invoke any error
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid Data", customErrs);
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Total {0} item have been unshelved successfully
                return new ServiceResult(ResultCodeConst.LibraryItem_Success0004,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Success0004),
                        entities.Count.ToString()));
            }

            // Msg: Unshelving the item was unsuccessfully
            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0004));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update range item instance status");
        }
    }
    
    public async Task<IServiceResult> SoftDeleteAsync(int libraryItemInstanceId)
    {
        try
        {
            // Determine current lang context 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Retrieve the entity
            // Build specification query
            var baseSpec =
                new BaseSpecification<LibraryItemInstance>(x => x.LibraryItemInstanceId == libraryItemInstanceId);
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item instance" : "bản sao"));
            }

            var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(bRec => 
                        bRec.BorrowRecordDetails.Any(brd => 
                            brd.LibraryItemInstanceId == libraryItemInstanceId && // Exist in any borrow record details
                            brd.Status != BorrowRecordStatus.Returned)) // Exclude elements with returned status
                ).Data is true; // Convert object to boolean 
                        
            if (hasBorrowRecordConstraint) // Has any constraint 
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008));
            }

            // Only library item copy with status OutOfShelf allowed to delete
            if (existingEntity.Status != nameof(LibraryItemInstanceStatus.OutOfShelf))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0009));
            }

            // Update delete status
            existingEntity.IsDeleted = true;

            // Update inventory
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == existingEntity.LibraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }

            // Reduce total copy number
            currentInventory.TotalUnits -= 1;

            // Process update library item copy
            await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(existingEntity);

            // Process update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process soft delete library item instance");
        }
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds)
    {
        try
        {
            // Get all matching library item instance 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(e =>
                // With specific library item
                e.LibraryItemId == libraryItemId &&
                // Any id match request list
                libraryItemInstanceIds.Contains(e.LibraryItemInstanceId));
            var itemInstanceEntities = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var itemInstanceList = itemInstanceEntities.ToList();
            if (!itemInstanceList.Any()) // Check whether not exist any item
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            if (itemInstanceList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Add custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Progress update deleted status to true
            for (int i = 0; i < itemInstanceList.Count; i++)
            {
                var ec = itemInstanceList[i];

                var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(bRec => 
                            bRec.BorrowRecordDetails.Any(brd => 
                                brd.LibraryItemInstanceId == ec.LibraryItemInstanceId && // Exist in any borrow record details
                                brd.Status != BorrowRecordStatus.Returned)) // Exclude elements with returned status
                    ).Data is true; // Convert object to boolean 
                
                if (hasBorrowRecordConstraint) // Has any constraint 
                {
                    // Add error
                    customErrs.Add($"ids[{i}]",
                        [await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008)]);
                }
            }

            if (customErrs.Any()) // Invoke errors
            {
                throw new UnprocessableEntityException("Invalid data", customErrs);
            }

            // Only library item copy with status OutOfShelf allowed to delete
            if (itemInstanceList.Any(be => be.Status != nameof(LibraryItemInstanceStatus.OutOfShelf)))
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0009));
            }

            // Change delete status
            itemInstanceList.ForEach(x => { x.IsDeleted = true; });

            // Update current total to inventory
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == libraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }

            // Reduce total copy in inventory with all instances have been deleted above
            currentInventory.TotalUnits -= itemInstanceList.Count;

            // Update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0007,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when remove range library item instance");
        }
    }

    public async Task<IServiceResult> UndoDeleteAsync(int libraryItemInstanceId)
    {
        try
        {
            // Determine current lang context 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library item copy
            var existingEntity =
                await _unitOfWork.Repository<LibraryItemInstance, int>().GetByIdAsync(libraryItemInstanceId);
            // Check if library item already mark as deleted
            if (existingEntity == null || !existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item copy" : "bản sao"));
            }

            // Update delete status
            existingEntity.IsDeleted = false;

            // Update inventory
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == existingEntity.LibraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }

            // Reduce total instance number
            currentInventory.TotalUnits += 1;

            // Process update library item instance
            await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(existingEntity);

            // Process update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete library item instance");
        }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds)
    {
        try
        {
            // Get all matching library item instance 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(e =>
                // With specific library item
                e.LibraryItemId == libraryItemId &&
                // Any id match request
                libraryItemInstanceIds.Contains(e.LibraryItemInstanceId));
            var editionCopyEntities = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var editionCopyList = editionCopyEntities.ToList();
            if (!editionCopyList.Any()) // Check whether not exist any item
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            if (editionCopyList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Progress undo deleted status to false
            editionCopyList.ForEach(x => x.IsDeleted = false);

            // Update current total to inventory
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(
                new BaseSpecification<LibraryItemInventory>(
                    // With specific library item id
                    x => x.LibraryItemId == libraryItemId))).Data as LibraryItemInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
                // An error occurred while updating the inventory data
                return new ServiceResult(ResultCodeConst.LibraryItem_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0001));
            }

            // Reversed total copy in inventory with all instances have been reversed above
            currentInventory.TotalUnits += editionCopyList.Count;

            // Update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete range");
        }
    }

    public async Task<IServiceResult> DeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds)
    {
        try
        {
            // Get all matching library item instance 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(e =>
                // With specific library item
                e.LibraryItemId == libraryItemId &&
                // Any library item id match request list
                libraryItemInstanceIds.Contains(e.LibraryItemInstanceId));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(bec => bec.LibraryItemConditionHistories));
            var itemInstanceEntities = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var itemInstanceList = itemInstanceEntities.ToList();
            if (!itemInstanceList.Any()) // Check whether not exist any item
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            if (itemInstanceList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // All instance must have only one condition history enabling to perform delete
            if (itemInstanceList.Select(x => x.LibraryItemConditionHistories)
                .Any(x => x.Count > 1)) // Exist at least one not match require
            {
                // Return not allow to delete
                return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
            }
            else // All require match -> Process delete range
            {
                // Check whether total item condition monitoring
                foreach (var cc in itemInstanceList.SelectMany(x => x.LibraryItemConditionHistories))
                {
                    // Progress delete without save
                    await _conditionHistoryService.DeleteWithoutSaveChangesAsync(cc.ConditionHistoryId);
                }
            }

            // Process delete range
            await _unitOfWork.Repository<LibraryItemInstance, int>().DeleteRangeAsync(libraryItemInstanceIds.ToArray());
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, itemInstanceList.Count.ToString()), true);
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
            throw new Exception("Error invoke when process delete range library item instance");
        }
    }

    public async Task<IServiceResult> CountTotalItemInstanceAsync(int libraryItemId)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(x => x.LibraryItemId == libraryItemId);
            // Count all instance of specific item
            var totalInstanceOfItem = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(baseSpec);

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalInstanceOfItem);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process count total copy");
        }
    }

    public async Task<IServiceResult> CountTotalItemInstanceAsync(List<int> libraryItemIds)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(x => libraryItemIds.Contains(x.LibraryItemId));
            // Count all instance of specific item
            var totalInstanceOfItem = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(baseSpec);

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalInstanceOfItem);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process count total copy");
        }
    }
    
    public async Task<IServiceResult> UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
        List<int> libraryItemInstanceIds,
        LibraryItemInstanceStatus status,
        bool isProcessBorrowRequest)
    {
        try
        {
            // Determine current lang context 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemInstance>(li =>
                // All exist in request ids
                libraryItemInstanceIds.Contains(li.LibraryItemInstanceId));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(li => li.LibraryItem)
                    .ThenInclude(l => l.LibraryItemInventory!)
            );
            // Retrieve all with spec
            var entities = (await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetAllWithSpecAsync(baseSpec)).ToList();
            if (!entities.Any())
            {
                // Fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }
            
            // Iterate each instance to update status
            foreach (var instance in entities)
            {
                // Check current status
                var currStatus = instance.Status;
                
                // Assign inventory
                var inventory = instance.LibraryItem.LibraryItemInventory;
                
                // Handle update status for each type 
                switch (currStatus)
                {
                    case nameof(LibraryItemInstanceStatus.InShelf):
                        // Case 1: InShelf -> Borrowed (both online & in-person borrow)
                        if (status == LibraryItemInstanceStatus.Borrowed)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.Borrowed);
                            // Update circulation
                            instance.IsCirculated = true;
                            
                            // Update inventory quantity
                            if (inventory != null)
                            {
                                // Is process from borrow request
                                if (isProcessBorrowRequest && inventory.RequestUnits > 0)
                                {
                                    // Reduce request units
                                    inventory.RequestUnits--;
                                    // Increase borrowed units
                                    inventory.BorrowedUnits++;
                                }
                                else if(!isProcessBorrowRequest && inventory.AvailableUnits > 0)
                                {
                                    // Reduce available units
                                    inventory.AvailableUnits--;
                                    // Increase borrowed units
                                    inventory.BorrowedUnits++;
                                }
                            }
                        }
                        // Case 2: InShelf -> OutOfShelf
                        if (status == LibraryItemInstanceStatus.OutOfShelf)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                            
                            if (inventory != null && inventory.AvailableUnits > 0)
                            {
                                // Reduce available units
                                inventory.AvailableUnits--;
                            }
                        }
                        // Case 3: InShelf -> InShelf (no effect)
                        // Case 4: InShelf -> Reserved (not allow)
                        // Case 5: InShelf -> Lost (not allow)
                        break;
                    case nameof(LibraryItemInstanceStatus.OutOfShelf):
                        // Case 1: OutOfShelf -> InShelf
                        if (status == LibraryItemInstanceStatus.InShelf)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.InShelf);
                            
                            // Required to have a status
                            if (instance.LibraryItem.Shelf == null)
                            {
                                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                                // Required shelf location
                                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                                    StringUtils.Format(errMsg, isEng
                                        ? $"Shelf location not found for item '{instance.LibraryItem.Title}'"
                                        : $"Không tìm thấy vị trí kệ cho tài liệu '{instance.LibraryItem.Title}'"));
                            }

                            if (inventory != null)
                            {
                                // Increase availability
                                inventory.AvailableUnits++;
                            }
                        }
                        // Case 2: OutOfShelf -> OutOfShelf (no effect)
                        // Case 3: OutOfShelf -> Borrowed (not allow)
                        // Case 4: OutOfShelf -> Reserved (assign return item to reservation)
                        if (status == LibraryItemInstanceStatus.Reserved)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.Reserved);
                        }
                        // Case 5: OutOfShelf -> Lost (not allow)
                        break;
                    case nameof(LibraryItemInstanceStatus.Borrowed):
                        // Case 1: Borrowed -> InShelf (Not allow)
                        // Case 2: Borrowed -> OutOfShelf (return item) 
                        if (status == LibraryItemInstanceStatus.OutOfShelf)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                            
                            if (inventory != null && inventory.BorrowedUnits > 0)
                            {
                                // Reduce borrow units
                                inventory.BorrowedUnits--;
                            }
                        }
                        // Case 2: Borrowed -> Borrowed (no effect) 
                        // Case 4: Borrowed -> Reserved (assign item to reservation queue)
                        if (status == LibraryItemInstanceStatus.Reserved)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.Reserved);
                            
                            if (inventory != null && inventory.BorrowedUnits > 0)
                            {
                                // Reduce borrow units
                                inventory.BorrowedUnits--;
                                // Increase reserved units
                                inventory.ReservedUnits++;
                            }
                        }
                        // Case 5: Borrowed -> Lost
                        if (status == LibraryItemInstanceStatus.Lost)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.Lost);
                            
                            if (inventory != null && inventory.BorrowedUnits > 0 
                                                  && inventory.TotalUnits > 0)
                            {
                                // Reduce borrow units
                                inventory.BorrowedUnits--;
                                // Reduce total units
                                inventory.TotalUnits--;
                                // Increase lost units 
                                inventory.LostUnits++;
                            }
                        }
                        break;
                    case nameof(LibraryItemInstanceStatus.Reserved):
                        // Case 1: Reserved -> InShelf (not allow)
                        // Case 2: Reserved -> OutOfShelf (reservation expired but not found any other reservation)
                        if (status == LibraryItemInstanceStatus.OutOfShelf)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                            
                            if (inventory != null && inventory.ReservedUnits > 0)
                            {
                                // Reduce reserved units
                                inventory.ReservedUnits--;
                            }
                        }
                        // Case 3: Reserved -> Reserved (No effect)
                        // Case 4: Reserved -> Borrowed (reservation's person comes to pick up item)
                        if (status == LibraryItemInstanceStatus.Borrowed)
                        {
                            // Update status
                            instance.Status = nameof(LibraryItemInstanceStatus.Borrowed);
                            
                            if (inventory != null && inventory.ReservedUnits > 0)
                            {
                                // Reduce reserved units
                                inventory.ReservedUnits--;
                                // Increase borrowed unties
                                inventory.BorrowedUnits++;
                            }
                        }
                        // Case 5: Reserved -> Lost (not allow)
                        break;
                    case nameof(LibraryItemInstanceStatus.Lost):
                        // Not allow to update from lost status to other statuses
                        break;
                }
                
                // Process update
                await _unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(instance);
                
                // Progress update can borrow status of library item
                if (inventory != null)
                {
                    await _libItemService.UpdateBorrowStatusWithoutSaveChangesAsync(
                        id: inventory.LibraryItemId,
                        canBorrow: inventory.AvailableUnits > 0);
                }
            }
            
            // Updated without save change
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update range status with item's inventory");
        }
    }
}