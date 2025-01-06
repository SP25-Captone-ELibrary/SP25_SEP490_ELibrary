using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.BookEditions;
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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nest;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class BookEditionCopyService : GenericService<BookEditionCopy, BookEditionCopyDto, int>,
    IBookEditionCopyService<BookEditionCopyDto>
{
    private readonly IBookEditionService<BookEditionDto> _editionService;
    private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
    private readonly IBookEditionInventoryService<BookEditionInventoryDto> _inventoryService;
    private readonly ICopyConditionHistoryService<CopyConditionHistoryDto> _conditionHistoryService;

    public BookEditionCopyService(
        IBookEditionService<BookEditionDto> editionService,
        IBookEditionInventoryService<BookEditionInventoryDto> inventoryService,
        ICopyConditionHistoryService<CopyConditionHistoryDto> conditionHistoryService,
        ILibraryShelfService<LibraryShelfDto> libShelfService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _editionService = editionService;
        _libShelfService = libShelfService;
        _inventoryService = inventoryService;
        _conditionHistoryService = conditionHistoryService;
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
		    // Determine current lang context
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;

		    // Build a base specification to filter by BookEditionCopyId
		    var baseSpec = new BaseSpecification<BookEditionCopy>(a => a.BookEditionCopyId == id);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(bec => bec.CopyConditionHistories));

		    // Retrieve book edition copy with specification
		    var editionCopyEntity = await _unitOfWork.Repository<BookEditionCopy, int>().GetWithSpecAsync(baseSpec);
		    if (editionCopyEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book edition copy" : "bản in"));
		    }

		    // Check whether book edition copy in the trash bin
		    if (!editionCopyEntity.IsDeleted)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Check whether total copy condition monitoring
		    // history equal 1 (default book condition when create)
		    if (editionCopyEntity.CopyConditionHistories.Count == 1)
		    {
			    // Progress delete without save
			    await _conditionHistoryService.DeleteWithoutSaveChangesAsync(
				    // Retrieve first element id
				    editionCopyEntity.CopyConditionHistories.First().ConditionHistoryId);
		    } // Else: do not allow to delete

		    // Process add delete entity
		    await _unitOfWork.Repository<BookEditionCopy, int>().DeleteAsync(id);
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
    
    public override async Task<IServiceResult> UpdateAsync(int id, BookEditionCopyDto dto)
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
			var baseSpec = new BaseSpecification<BookEditionCopy>(x => x.BookEditionCopyId == id);
			// Include borrow records and requests relation
			baseSpec.ApplyInclude(q => q
				// Include book edition
				.Include(bec => bec.BookEdition)
				// Include borrow records
				.Include(bec => bec.BorrowRecords)
				// Include borrow requests
				.Include(bec => bec.BorrowRequests)
			);
			var existingEntity = await _unitOfWork.Repository<BookEditionCopy, int>()
				.GetWithSpecAsync(baseSpec);
			if (existingEntity == null || existingEntity.IsDeleted)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "book edition copy" : "bản in"));
			}
			
			// Validate status
			Enum.TryParse(typeof(BookEditionCopyStatus), dto.Status, out var validStatus);
			if (validStatus == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "any status match to process update" : "trạng thái phù hợp"));
			}
			else
			{
				// Parse into enum
				var enumStatus = (BookEditionCopyStatus) validStatus;
				
				// Do not allow to update BORROWED/RESERVED status
				// With RESERVED status of edition copy, it will change automatically when 
				// someone return their borrowed book and assigned that book to others, who are in reservation queue
				if (enumStatus == BookEditionCopyStatus.Borrowed || enumStatus == BookEditionCopyStatus.Reserved)
				{
					// Fail to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}else if (enumStatus == BookEditionCopyStatus.InShelf)
				{
					// Required exist shelf location in book edition for update to in-shelf status
					if (existingEntity.BookEdition.ShelfId == null || existingEntity.BookEdition.ShelfId == 0)
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0011);
						// Required shelf location
						return new ServiceResult(ResultCodeConst.Book_Warning0011,
							StringUtils.Format(errMsg, isEng 
								? "Shelf location not found" : "Không tìm thấy vị trí kệ cho sách"));
					}
				}	
			}
			
			// Check whether edition copy is borrowed or reserved
			var hasConstraint = 
				existingEntity.BorrowRecords.Any(br => 
					// Check any record not current in RETURNED status
					br.Status != nameof(BorrowRecordStatus.Returned)) || 
				existingEntity.BorrowRequests.Any(br => 
					// Check any request not current in REJECTED or CANCELLED status
					br.Status != nameof(BorrowRequestStatus.Rejected) && br.Status != nameof(BorrowRequestStatus.Cancelled));
			if (hasConstraint) // Has any constraint 
			{
				return new ServiceResult(ResultCodeConst.Book_Warning0008,
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008));
			}
			
			// Process update status
			existingEntity.Status = dto.Status;
			
			// Check if there are any differences between the original and the updated entity
			if (!_unitOfWork.Repository<BookEditionCopy, int>().HasChanges(existingEntity))
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
			}

			// Retrieve current inventory data
			var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
				// With specific book edition id
				x => x.BookEditionId == existingEntity.BookEditionId))).Data as BookEditionInventoryDto;
			if (currentInventory == null) // Not found inventory
			{
				// An error occurred while updating the inventory data
				return new ServiceResult(ResultCodeConst.Book_Fail0001, 
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
			}
			
			// Check for typeof status update
			if ((BookEditionCopyStatus)validStatus == BookEditionCopyStatus.OutOfShelf)
			{
				// Reduce current available value
				currentInventory.AvailableCopies -= 1;
			}else if ((BookEditionCopyStatus)validStatus == BookEditionCopyStatus.InShelf)
			{
				// Increase current available value
				currentInventory.AvailableCopies += 1;
			}
			
			// Progress update when all require passed
			await _unitOfWork.Repository<BookEditionCopy, int>().UpdateAsync(existingEntity);

			// Progress update inventory
			await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
			
			// Progress update can borrow status of book edition
			await _editionService.UpdateBorrowStatusWithoutSaveChangesAsync(
				id: existingEntity.BookEditionId,
				canBorrow: currentInventory.AvailableCopies > 0);
			
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
    
    public async Task<IServiceResult> AddRangeToBookEditionAsync(int bookEditionId, List<BookEditionCopyDto> editionCopies)
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
            for (int i = 0; i < editionCopies.Count; i++)
            {
	            // Validate condition status (check for first only)
	            var conditionHistories = editionCopies[i].CopyConditionHistories.ToList();
	            if (conditionHistories.Count > 1)
	            {
					return new ServiceResult(ResultCodeConst.SYS_Fail0001, isEng 
							? "Not allow to add multiple condition histories" 
							: "Không được thêm nhiều trạng thái bản in ban đầu");		            
	            }
	            else
	            {
		            if (!Enum.TryParse(typeof(ConditionHistoryStatus),
			                conditionHistories[0].Condition, out _)) // Not valid status
		            {
			            // Add error 
			            customErrors.Add(
				            $"bookEditionCopies[{i}].conditionStatus", 
				            [isEng ? "Condition status not value" : "Trạng thái điều kiện không hợp lệ"]);
		            };
	            }
	            
                if (uniqueList.Add(editionCopies[i].Barcode)) // Valid barcode
                {
                    // Check exist code in DB
                    var isExist = await _unitOfWork.Repository<BookEditionCopy, int>().AnyAsync(x => 
	                    x.Barcode == editionCopies[i].Barcode);
                    if (isExist) // already exist
                    {
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0006);
                        // Add error 
                        customErrors.Add(
                            $"bookEditionCopies[{i}].barcode", 
                            [StringUtils.Format(errMsg, $"'{editionCopies[i].Barcode}'")]);
                    }
                }
                else
                {
                    // Add error 
                    customErrors.Add(
                        $"bookEditionCopies[{i}].barcode", 
                        [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0005)]);                    
                }
            }
            
            // Check if any error invoke
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }            
            
            // Check exist book edition
            var bookEditionEntity = (await _editionService.GetByIdAsync(bookEditionId)).Data as BookEditionDto;
            if (bookEditionEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "book edition" : "ấn bản"));
            }
            
            var toAddCopies = new List<BookEditionCopy>();
            // Process add new edition copy 
            editionCopies.ForEach(bec =>
            {
                toAddCopies.Add(new()
                {
                    // Assign to specific book edition
                    BookEditionId = bookEditionEntity.BookEditionId,
                    // Assign copy barcode
                    Barcode = bec.Barcode,
                    // Default status
                    Status = nameof(BookEditionCopyStatus.OutOfShelf),
                    // Boolean 
                    IsDeleted = false,
                    // Condition histories
                    CopyConditionHistories = _mapper.Map<List<CopyConditionHistory>>(bec.CopyConditionHistories)
                });
            });
            
            // Add range 
            await _unitOfWork.Repository<BookEditionCopy, int>().AddRangeAsync(toAddCopies);
            
            // Update inventory total
            // Get inventory by book edition id
            var getInventoryRes = await _inventoryService.GetWithSpecAsync(
	            new BaseSpecification<BookEditionInventory>(
	            x => x.BookEditionId == bookEditionId), tracked: false);
            if (getInventoryRes.Data is BookEditionInventoryDto inventoryDto) // Get data success
            {
	            // Set relations to null
	            inventoryDto.BookEdition = null!;
	            // Update total
	            inventoryDto.TotalCopies += toAddCopies.Count;
	            
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
            throw new Exception("Error invoke when process add range copy to book edition");
        }
    }
		
	public async Task<IServiceResult> UpdateRangeAsync(int bookEditionId, List<int> bookEditionCopyIds, string status)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            // Validate status type
            Enum.TryParse(typeof(BookEditionCopyStatus), status, out var validStatus);
            if (validStatus == null)
            {
                customErrors.Add(
                    "status", [isEng ? "Invalid status selection" : "Trạng thái được chọn không hợp lệ"]);
            }
            else
            {
	            // Parse into enum
	            var enumStatus = (BookEditionCopyStatus) validStatus;
				
	            // Do not allow to update BORROWED/RESERVED status
	            // With RESERVED status of edition copy, it will change automatically when 
	            // someone return their borrowed book and assigned that book to others, who are in reservation queue
	            if (enumStatus == BookEditionCopyStatus.Borrowed || enumStatus == BookEditionCopyStatus.Reserved)
	            {
		            // Fail to update
		            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
	            }	
            }

            // Check exist ids
            if (!bookEditionCopyIds.Any())
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "any book edition copy match" : "các ấn bảng cần sửa"));
            }
            
            // Check exist each id in collection
            for (int i = 0; i < bookEditionCopyIds.Count; i++)
            {
	            // Build spec base
	            var editionCopySpec = new BaseSpecification<BookEditionCopy>(
		            x => x.BookEditionCopyId == bookEditionCopyIds[i]);
	            editionCopySpec.ApplyInclude(q => q
		            // Include borrow requests 
		            .Include(ec => ec.BorrowRequests)
		            // Include borrow records
		            .Include(ec => ec.BorrowRecords)
	            );
	            // Get book edition copy by id and include constraints
                var bookEditionCopy = await _unitOfWork.Repository<BookEditionCopy, int>().GetWithSpecAsync(editionCopySpec);
                if (bookEditionCopy == null) // not exist
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    // Add error 
                    customErrors.Add($"bookEditionCopyIds[{i}]",
                        [StringUtils.Format(errMsg, isEng ? "edition copy" : "bản in")]);
                }
                else
                {
                    // Check whether edition copy is borrowed or reserved
                    var hasConstraint = 
	                    bookEditionCopy.BorrowRecords.Any(br => 
		                    // Check any record not current in RETURNED status
		                    br.Status != nameof(BorrowRecordStatus.Returned)) || 
	                    bookEditionCopy.BorrowRequests.Any(br => 
		                    // Check any request not current in REJECTED or CANCELLED status
		                    br.Status != nameof(BorrowRequestStatus.Rejected) && br.Status != nameof(BorrowRequestStatus.Cancelled));
                    if (hasConstraint) // Has any constraint 
                    {
	                    // Add error 
	                    customErrors.Add($"bookEditionCopyIds[{i}]", [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008)]);
                    }
                }
            }

            // Check if any error invoke
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }

            // Build spec to retrieve all book edition copy match
            var baseSpec = new BaseSpecification<BookEditionCopy>(x =>
		            // Must update within the same book edition
					x.BookEditionId == bookEditionId &&
		            // Exist in update list
					bookEditionCopyIds.Contains(x.BookEditionCopyId)); 
            // Apply include 
            baseSpec.ApplyInclude(q => q.Include(bec => bec.BookEdition));
            // Get all book edition copy with spec
            var editionCopyEntities =
                await _unitOfWork.Repository<BookEditionCopy, int>().GetAllWithSpecAsync(baseSpec);
            // Convert to list collection
            var editionCopyList = editionCopyEntities.ToList();
            if (!editionCopyList.Any() // Not found any
                || editionCopyList.Any(x => x.IsDeleted)) // Some are mark as deleted
            {
                // Mark as fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
            else // Exist any
            {
	            // Require all book edition have shelf location if update to status in-shelf
	            if ((BookEditionCopyStatus?) validStatus == BookEditionCopyStatus.InShelf)
	            {
					// Required exist shelf location in book edition for update to in-shelf status
					if (editionCopyList.Select(x => x.BookEdition.ShelfId).Any(shelfId => shelfId == null))
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0011);
						// Required shelf location
						return new ServiceResult(ResultCodeConst.Book_Warning0011,
							StringUtils.Format(errMsg, isEng 
								? "Shelf location not found" : "Không tìm thấy vị trí kệ"));
					}
				}
            }
            
            // Retrieve current inventory data
            var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
            	// With specific book edition id
            	x => x.BookEditionId == bookEditionId))).Data as BookEditionInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
            	// An error occurred while updating the inventory data
            	return new ServiceResult(ResultCodeConst.Book_Fail0001, 
            		await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
            }
            
            // Process update edition copy status 
            foreach (var bec in editionCopyList)
            {
				// Check whether status is change 
				if (!Equals(bec.Status, status)) // Compare current with to update status
				{
					// Check for typeof status update
					if ((BookEditionCopyStatus?)validStatus == BookEditionCopyStatus.OutOfShelf)
					{
						// Reduce current available value
						currentInventory.AvailableCopies -= 1;
					}else if ((BookEditionCopyStatus?)validStatus == BookEditionCopyStatus.InShelf)
					{
						// Increase current available value
						currentInventory.AvailableCopies += 1;
					}
					
				}
				
				// Assign update status 
                bec.Status = status;
            }

            // Progress update inventory
            await _inventoryService.UpdateWithoutSaveChangesAsync(currentInventory);
            
            // Progress update can borrow status of book edition
            await _editionService.UpdateBorrowStatusWithoutSaveChangesAsync(
	            id: bookEditionId,
	            canBorrow: currentInventory.AvailableCopies > 0);
            
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
            throw new Exception("Error invoke when process update range book edition copy");
        }
    }
		
	public async Task<IServiceResult> SoftDeleteAsync(int bookEditionCopyId)
	{
		try
		{
			// Determine current lang context 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;

			// Retrieve the entity
			// Build specification query
			var baseSpec = new BaseSpecification<BookEditionCopy>(x => x.BookEditionCopyId == bookEditionCopyId);
			// Include borrow records and requests relation
			baseSpec.ApplyInclude(q => q
				// Include borrow records
				.Include(bec => bec.BorrowRecords)
				// Include borrow requests
				.Include(bec => bec.BorrowRequests)
			);
			var existingEntity = await _unitOfWork.Repository<BookEditionCopy, int>()
				.GetWithSpecAsync(baseSpec);
			if (existingEntity == null || existingEntity.IsDeleted)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "book edition copy" : "bản in"));
			}

			// Check whether edition copy is borrowed or reserved
			var hasConstraint =
				existingEntity.BorrowRecords.Any(br =>
					// Check any record not current in RETURNED status
					br.Status != nameof(BorrowRecordStatus.Returned)) ||
				existingEntity.BorrowRequests.Any(br =>
					// Check any request not current in REJECTED or CANCELLED status
					br.Status != nameof(BorrowRequestStatus.Rejected) &&
					br.Status != nameof(BorrowRequestStatus.Cancelled));
			if (hasConstraint) // Has any constraint 
			{
				return new ServiceResult(ResultCodeConst.Book_Warning0008,
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008));
			}

			// Only book edition copy with status OutOfShelf allowed to delete
			if (existingEntity.Status != nameof(BookEditionCopyStatus.OutOfShelf))
			{
				return new ServiceResult(ResultCodeConst.Book_Warning0009,
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0009));
			}

			// Update delete status
			existingEntity.IsDeleted = true;
			
			// Update inventory
			// Retrieve current inventory data
			var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
				// With specific book edition id
				x => x.BookEditionId == existingEntity.BookEditionId))).Data as BookEditionInventoryDto;
			if (currentInventory == null) // Not found inventory
			{
				// An error occurred while updating the inventory data
				return new ServiceResult(ResultCodeConst.Book_Fail0001, 
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
			}
			
			// Reduce total copy number
			currentInventory.TotalCopies -= 1;
			
			// Process update book edition copy
			await _unitOfWork.Repository<BookEditionCopy, int>().UpdateAsync(existingEntity);
			
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
			throw new Exception("Error invoke when process soft delete book edition copy");	
		}
	}
	
	public async Task<IServiceResult> SoftDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds)
	{
		try
		{
			// Get all matching book edition copy 
			// Build spec
			var baseSpec = new BaseSpecification<BookEditionCopy>(e =>
					// With specific book edition
					e.BookEditionId == bookEditionId &&
					// Any id match request list
					bookEditionCopyIds.Contains(e.BookEditionCopyId));
			// Include borrow records and requests relation
			baseSpec.ApplyInclude(q => q
				// Include borrow records
				.Include(bec => bec.BorrowRecords)
				// Include borrow requests
				.Include(bec => bec.BorrowRequests)
			);
			var editionCopyEntities = await _unitOfWork.Repository<BookEditionCopy, int>()
				.GetAllWithSpecAsync(baseSpec);
			// Check if any data already soft delete
			var editionCopyList = editionCopyEntities.ToList();
			if (!editionCopyList.Any()) // Check whether not exist any item
			{
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}
			if (editionCopyList.Any(x => x.IsDeleted))
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
			}

			// Add custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Progress update deleted status to true
			for (int i = 0; i < editionCopyList.Count; i++)
			{
				var ec = editionCopyList[i];
				
				// Check whether edition copy is borrowed or reserved
				var hasConstraint =
					ec.BorrowRecords.Any(br =>
						// Check any record not current in RETURNED status
						br.Status != nameof(BorrowRecordStatus.Returned)) ||
					ec.BorrowRequests.Any(br =>
						// Check any request not current in REJECTED or CANCELLED status
						br.Status != nameof(BorrowRequestStatus.Rejected) &&
						br.Status != nameof(BorrowRequestStatus.Cancelled));
				if (hasConstraint) // Has any constraint 
				{
					// Add error
					customErrs.Add($"Ids[{i}]", [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008)]);
				}
			}

			if (customErrs.Any()) // Invoke errors
			{
				throw new UnprocessableEntityException("Invalid data", customErrs);
			}

			// Only book edition copy with status OutOfShelf allowed to delete
			if (editionCopyList.Any(be => be.Status != nameof(BookEditionCopyStatus.OutOfShelf)))
			{
				return new ServiceResult(ResultCodeConst.Book_Warning0009,
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0009));
			}
			
			// Change delete status
			editionCopyList.ForEach(x => { x.IsDeleted = true; });
			
			// Update current total to inventory
			// Retrieve current inventory data
			var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
				// With specific book edition id
				x => x.BookEditionId == bookEditionId))).Data as BookEditionInventoryDto;
			if (currentInventory == null) // Not found inventory
			{
				// An error occurred while updating the inventory data
				return new ServiceResult(ResultCodeConst.Book_Fail0001, 
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
			}
			
			// Reduce total copy in inventory with all copies have been deleted above
			currentInventory.TotalCopies -= editionCopyList.Count;

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
            throw new Exception("Error invoke when remove range book edition copy");
        }
	}
	
	public async Task<IServiceResult> UndoDeleteAsync(int bookEditionCopyId)
	{
		try
		{
            // Determine current lang context 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
			// Check exist book edition copy
			var existingEntity = await _unitOfWork.Repository<BookEditionCopy, int>().GetByIdAsync(bookEditionCopyId);
			// Check if book edition already mark as deleted
			if (existingEntity == null || !existingEntity.IsDeleted)
			{
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "book edition copy" : "bản in"));
			}
			
			// Update delete status
			existingEntity.IsDeleted = false;
			
			// Update inventory
			// Retrieve current inventory data
			var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
				// With specific book edition id
				x => x.BookEditionId == existingEntity.BookEditionId))).Data as BookEditionInventoryDto;
			if (currentInventory == null) // Not found inventory
			{
				// An error occurred while updating the inventory data
				return new ServiceResult(ResultCodeConst.Book_Fail0001, 
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
			}
			
			// Reduce total copy number
			currentInventory.TotalCopies += 1;
			
			// Process update book edition copy
			await _unitOfWork.Repository<BookEditionCopy, int>().UpdateAsync(existingEntity);
			
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
			throw new Exception("Error invoke when process undo delete book edition copy");	
		}
	}

	public async Task<IServiceResult> UndoDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds)
	{
		try
        {
            // Get all matching book edition copy 
            // Build spec
            var baseSpec = new BaseSpecification<BookEditionCopy>(e => 
	            // With specific book edition
	            e.BookEditionId == bookEditionId &&
	            // Any id match request
	            bookEditionCopyIds.Contains(e.BookEditionCopyId));
            var editionCopyEntities = await _unitOfWork.Repository<BookEditionCopy, int>()
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
            var currentInventory = (await _inventoryService.GetWithSpecAsync(new BaseSpecification<BookEditionInventory>(
            	// With specific book edition id
            	x => x.BookEditionId == bookEditionId))).Data as BookEditionInventoryDto;
            if (currentInventory == null) // Not found inventory
            {
            	// An error occurred while updating the inventory data
            	return new ServiceResult(ResultCodeConst.Book_Fail0001, 
            		await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0001));
            }
            
            // Reserved total copy in inventory with all copies have been reversed above
            currentInventory.TotalCopies += editionCopyList.Count;

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
    
	public async Task<IServiceResult> DeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds)
    {
        try
        {
            // Get all matching book edition copy 
            // Build spec
            var baseSpec = new BaseSpecification<BookEditionCopy>(e => 
	            // With specific book edition
	            e.BookEditionId == bookEditionId &&
	            // Any book edition id match request list
	            bookEditionCopyIds.Contains(e.BookEditionCopyId));
            // Apply include
            baseSpec.ApplyInclude(q => q
	            .Include(bec => bec.CopyConditionHistories));
            var editionCopyEntities = await _unitOfWork.Repository<BookEditionCopy, int>()
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
            
            // All copy must have only one condition copy history enabling to perform delete
            if (editionCopyList.Select(x => x.CopyConditionHistories)
                .Any(x => x.Count > 1)) // Exist at least one not match require
            {
	            // Return not allow to delete
	            return new ServiceResult(ResultCodeConst.SYS_Fail0007,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
            }
            else // All require match -> Process delete range
            {
	            // Check whether total copy condition monitoring
	            foreach (var cc in editionCopyList.SelectMany(x => x.CopyConditionHistories))
	            {
		            // Progress delete without save
		            await _conditionHistoryService.DeleteWithoutSaveChangesAsync(cc.ConditionHistoryId);
	            }
            }

            // Process delete range
            await _unitOfWork.Repository<BookEditionCopy, int>().DeleteRangeAsync(bookEditionCopyIds.ToArray());
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
            	var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
            	return new ServiceResult(ResultCodeConst.SYS_Success0008,
            		StringUtils.Format(msg, editionCopyList.Count.ToString()), true);
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
            throw new Exception("Error invoke when process delete range book edition copy");
        }
    }

	public async Task<IServiceResult> CountTotalEditionCopyAsync(int bookEditionId)
	{
		try
        {
            // Build spec
            var baseSpec = new BaseSpecification<BookEditionCopy>(x => x.BookEditionId == bookEditionId);
			// Count all book copy of specific edition
            var totalCopyOfEdition = await _unitOfWork.Repository<BookEditionCopy, int>().CountAsync(baseSpec);
        	
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalCopyOfEdition);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process count total copy");
        } 
	}
	
	public async Task<IServiceResult> CountTotalEditionCopyAsync(List<int> bookEditionIds)
	{
		try
		{
			// Build spec
			var baseSpec = new BaseSpecification<BookEditionCopy>(x => bookEditionIds.Contains(x.BookEditionId));
			// Count all book copy of specific edition
			var totalCopyOfEdition = await _unitOfWork.Repository<BookEditionCopy, int>().CountAsync(baseSpec);
        	
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalCopyOfEdition);
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process count total copy");
		} 
	}
}