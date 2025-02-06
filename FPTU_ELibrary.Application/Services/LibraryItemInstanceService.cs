using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
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
using MapsterMapper;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nest;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemInstanceService : GenericService<LibraryItemInstance, LibraryItemInstanceDto, int>,
    ILibraryItemInstanceService<LibraryItemInstanceDto>
{
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqService;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecService;
    private readonly ILibraryItemService<LibraryItemDto> _libItemService;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventoryService;
    private readonly ILibraryItemConditionHistoryService<LibraryItemConditionHistoryDto> _conditionHistoryService;

    public LibraryItemInstanceService(
        IBorrowRequestService<BorrowRequestDto> borrowReqService,
        IBorrowRecordService<BorrowRecordDto> borrowRecService,
        ILibraryItemService<LibraryItemDto> libItemService,
        ILibraryItemInventoryService<LibraryItemInventoryDto> inventoryService,
        ILibraryItemConditionHistoryService<LibraryItemConditionHistoryDto> conditionHistoryService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _borrowReqService = borrowReqService;
        _borrowRecService = borrowRecService;
        _libItemService = libItemService;
        _inventoryService = inventoryService;
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
                    // Include category
                    .ThenInclude(li => li.Category)
            );
            var existingEntity = await _unitOfWork.Repository<LibraryItemInstance, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library item copy" : "bản in"));
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
                // Do not allow to update BORROWED/RESERVED status
                // With RESERVED status of item instance, it will change automatically when 
                // someone return their borrowed book and assigned that book to others, who are in reservation queue
                if (enumStatus == LibraryItemInstanceStatus.Borrowed ||
                    enumStatus == LibraryItemInstanceStatus.Reserved)
                {
                    // Fail to update
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                }
                else if (enumStatus == LibraryItemInstanceStatus.InShelf)
                {
                    // Required exist shelf location in library item for update to in-shelf status
                    if (existingEntity.LibraryItem.ShelfId == null || existingEntity.LibraryItem.ShelfId == 0)
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                        // Required shelf location
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                            StringUtils.Format(errMsg, isEng
                                ? "Shelf location not found"
                                : "Không tìm thấy vị trí kệ cho sách"));
                    }
                }

                // Check whether item instance is borrowed or reserved
                var hasBorrowRequestConstraint = (await _borrowReqService.AnyAsync(
                    br => br.Status != BorrowRequestStatus.Expired && // Exclude elements with expired status
                          br.Status != BorrowRequestStatus.Cancelled && // Exclude elements with cancelled status
                          br.BorrowRequestDetails.Any(brd => // Exist in any borrow request details
                              brd.LibraryItem.LibraryItemInstances.Any(li => li.LibraryItemInstanceId == id)) // With specific instance 
                )).Data is true; // Convert object to boolean 
                
                var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(
                    bRec => bRec.Status != BorrowRecordStatus.Returned && // Exclude elements with returned status
                            bRec.BorrowRecordDetails.Any(brd => brd.LibraryItemInstanceId == id)) // Exist in any borrow record details
                    ).Data is true; // Convert object to boolean 
                
                if (hasBorrowRequestConstraint || hasBorrowRecordConstraint) // Has any constraint 
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
            if ((LibraryItemInstanceStatus)validStatus == LibraryItemInstanceStatus.OutOfShelf)
            {
                // Reduce current available value
                currentInventory.AvailableUnits -= 1;
            }
            else if ((LibraryItemInstanceStatus)validStatus == LibraryItemInstanceStatus.InShelf)
            {
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

    public async Task<IServiceResult> AddRangeToLibraryItemAsync(int libraryItemId,
        List<LibraryItemInstanceDto> itemInstances)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

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
                        : "Không được thêm nhiều trạng thái bản in ban đầu");
                }
                else
                {
                    if (!Enum.TryParse(typeof(LibraryItemConditionStatus),
                            conditionHistories[0].Condition, out _)) // Not valid status
                    {
                        // Add error 
                        customErrors.Add(
                            $"libraryItemInstances[{i}].conditionStatus",
                            [isEng ? "Condition status not value" : "Trạng thái điều kiện không hợp lệ"]);
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
                        customErrors.Add(
                            $"libraryItemInstances[{i}].barcode",
                            [StringUtils.Format(errMsg, $"'{itemInstances[i].Barcode}'")]);
                    }
                }
                else
                {
                    // Add error 
                    customErrors.Add(
                        $"libraryItemInstances[{i}].barcode",
                        [
                            isEng
                                ? $"Barcode '{itemInstances[i].Barcode}' is duplicated"
                                : $"Số đăng ký cá biệt '{itemInstances[i].Barcode}' đã bị trùng"
                        ]);
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
                    var toUpdateStatus = (LibraryItemInstanceStatus)validStatus!;
                    // Check if status change
                    if (!Equals(itemInstanceEntity.Status, toUpdateStatus.ToString())) // Change detected
                    {
                        // Do not allow to update BORROWED/RESERVED status
                        // With RESERVED status of item instance, it will change automatically when 
                        // someone return their borrowed book and assigned that book to others, who are in reservation queue
                        if (toUpdateStatus == LibraryItemInstanceStatus.Borrowed ||
                            toUpdateStatus == LibraryItemInstanceStatus.Reserved)
                        {
                            // Error key
                            var key = $"libraryItemInstances.[{i}].status";
                            var msg = isEng ? "Invalid status selection" : "Trạng thái được chọn không hợp lệ";
                            // Add error dic 
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, key, msg);
                        }
                        else if (toUpdateStatus == LibraryItemInstanceStatus.InShelf)
                        {
                            // Required exist shelf location in library item for update to in-shelf status
                            if (itemInstanceEntity.LibraryItem.ShelfId == null || itemInstanceEntity.LibraryItem.ShelfId == 0)
                            {
                                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0011);
                                // Required shelf location
                                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0011,
                                    StringUtils.Format(errMsg, isEng
                                        ? "Shelf location not found"
                                        : "Không tìm thấy vị trí kệ cho sách"));
                            }
                        }
                        
                        // Check whether item instance is borrowed or reserved
                        var hasBorrowRequestConstraint = (await _borrowReqService.AnyAsync(
                            br => br.Status != BorrowRequestStatus.Expired && // Exclude elements with expired status
                                  br.Status != BorrowRequestStatus.Cancelled && // Exclude elements with cancelled status
                                  br.BorrowRequestDetails.Any(brd => // Exist in any borrow request details
                                      brd.LibraryItem.LibraryItemInstances.Any(li => li.LibraryItemInstanceId == itemInstanceEntity.LibraryItemInstanceId)) // With specific instance 
                        )).Data is true; // Convert object to boolean  
                
                        var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(
                                    bRec => bRec.Status != BorrowRecordStatus.Returned && // Exclude elements with returned status
                                            bRec.BorrowRecordDetails.Any(brd => brd.LibraryItemInstanceId == itemInstanceEntity.LibraryItemInstanceId)) // Exist in any borrow record details
                            ).Data is true; // Convert object to boolean 
                        
                        if (hasBorrowRequestConstraint || hasBorrowRecordConstraint) // Has any constraint 
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
                            currentInventory.AvailableUnits -= 1;
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

            // Check whether item instance is borrowed or reserved
            var hasBorrowRequestConstraint = (await _borrowReqService.AnyAsync(
                br => br.Status != BorrowRequestStatus.Expired && // Exclude elements with expired status
                      br.Status != BorrowRequestStatus.Cancelled && // Exclude elements with cancelled status
                      br.BorrowRequestDetails.Any(brd => // Exist in any borrow request details
                          brd.LibraryItem.LibraryItemInstances.Any(li => li.LibraryItemInstanceId == libraryItemInstanceId)) // With specific instance 
            )).Data is true; // Convert object to boolean  
                
            var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(
                        bRec => bRec.Status != BorrowRecordStatus.Returned && // Exclude elements with returned status
                                bRec.BorrowRecordDetails.Any(brd => brd.LibraryItemInstanceId == libraryItemInstanceId)) // Exist in any borrow record details
                ).Data is true; // Convert object to boolean 
                        
            if (hasBorrowRequestConstraint || hasBorrowRecordConstraint) // Has any constraint 
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

                // Check whether item instance is borrowed or reserved
                var hasBorrowRequestConstraint = (await _borrowReqService.AnyAsync(
                    br => br.Status != BorrowRequestStatus.Expired && // Exclude elements with expired status
                          br.Status != BorrowRequestStatus.Cancelled && // Exclude elements with cancelled status
                          br.BorrowRequestDetails.Any(brd => // Exist in any borrow request details
                              brd.LibraryItem.LibraryItemInstances.Any(li => li.LibraryItemInstanceId == ec.LibraryItemInstanceId)) // With specific instance 
                )).Data is true; // Convert object to boolean  
                
                var hasBorrowRecordConstraint = (await _borrowRecService.AnyAsync(
                            bRec => bRec.Status != BorrowRecordStatus.Returned && // Exclude elements with returned status
                                    bRec.BorrowRecordDetails.Any(brd => brd.LibraryItemInstanceId == ec.LibraryItemInstanceId)) // Exist in any borrow record details
                    ).Data is true; // Convert object to boolean 
                
                if (hasBorrowRequestConstraint || hasBorrowRecordConstraint) // Has any constraint 
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
}