using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
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
using FPTU_ELibrary.Domain.Specifications.Params;
using Google.Apis.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class WarehouseTrackingDetailService :
    GenericService<WarehouseTrackingDetail, WarehouseTrackingDetailDto, int>,
    IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>
{
    private readonly IWarehouseTrackingService<WarehouseTrackingDto> _trackingSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemService<LibraryItemDto> _itemSvc;

    public WarehouseTrackingDetailService(
	    ILibraryItemService<LibraryItemDto> itemSvc,
	    ICategoryService<CategoryDto> cateSvc,
	    IWarehouseTrackingService<WarehouseTrackingDto> trackingSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _cateSvc = cateSvc;
	    _itemSvc = itemSvc;
        _trackingSvc = trackingSvc;
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
			
			// Build specification
			var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x => x.TrackingDetailId == id);
			// Apply include
			baseSpec.ApplyInclude(q => q
				.Include(w => w.LibraryItem)
				.Include(w => w.Category)
			);
			// Retrieve entity with spec
			var existingEntity =
				await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết dữ liệu nhập kho"));
			}
			
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), _mapper.Map<WarehouseTrackingDetailDto>(existingEntity));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get warehouse tracking detail by id");
	    }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, WarehouseTrackingDetailDto dto)
    {
	    try
	    {
		    // Determine current system lang 
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
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x => x.TrackingDetailId == id);
		    // Apply include 
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.WarehouseTracking)
			    .Include(w => w.LibraryItem!)
		    );
		    // Retrieve warehouse tracking detail by id
		    var existingEntity = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
		    if (existingEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết theo dõi kho"));
		    }
		    else
		    {
			    // Check for updating detail of completed or cancelled warehouse tracking 
                if (existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Completed ||
                    existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Cancelled)
                {
                	// Msg: Cannot change data as warehouse tracking was completed or cancelled
                	return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
                		await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
                }
		    }
		    
		    // Check constraints
		    if (existingEntity.LibraryItemId != null)
		    {
			    // This action cannot be performed, as warehouse tracking detail already exist cataloged item 
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0006,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0006));
		    }
		    
		    // Custom errors
		    var customErrors = new Dictionary<string, string[]>();
		    // Check exist ISBN
		    if(!string.IsNullOrEmpty(dto.Isbn) && 
		       await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(w => 
			       Equals(dto.Isbn, w.Isbn) && 
			       w.TrackingDetailId != existingEntity.TrackingDetailId)) // Exclude current update entity
		    {
			    customErrors.Add(
				    StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
				    [isEng 
					    ? $"ISBN '{dto.Isbn}' already exist in warehouse tracking detail" 
					    : $"Mã ISBN '{dto.Isbn}' đã tồn tại trong chi tiết theo dõi kho"
				    ]);
		    }
		    
		    // Check whether invoke any error
		    if(customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);
		    
		    // Check whether update category
		    if (!Equals(existingEntity.CategoryId, dto.CategoryId) &&
		        existingEntity.LibraryItemId != null) // Exist specific item
		    {
			    // Update category must be the same with item
			    if (existingEntity.LibraryItem?.CategoryId != dto.CategoryId)
			    {
				    // Msg: The action cannot be performed as category of item and warehouse tracking detail is different
				    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0011,
					    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0011));
			    }
		    }
		    
		    // Progress update properties
		    existingEntity.ItemName = dto.ItemName;
		    existingEntity.ItemTotal = dto.ItemTotal;
		    existingEntity.Isbn = dto.Isbn;
		    existingEntity.UnitPrice = dto.UnitPrice;
		    existingEntity.TotalAmount = dto.TotalAmount;
		    existingEntity.Reason = dto.Reason;
		    existingEntity.CategoryId = dto.CategoryId;
		    
		    // Check if there are any differences between the original and the updated entity
		    if (!_unitOfWork.Repository<WarehouseTrackingDetail, int>().HasChanges(existingEntity))
		    {
			    // Return success if nothing change
			    return new ServiceResult(ResultCodeConst.SYS_Success0003,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
		    }
		    
		    // Progress update
		    await _unitOfWork.Repository<WarehouseTrackingDetail, int>().UpdateAsync(existingEntity);
		    // Save to DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    // Update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
		    }
		    
		    // Fail to update
		    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
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

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Build spec
			var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x => x.TrackingDetailId == id);
			// Apply include 
			baseSpec.ApplyInclude(q => q
				.Include(w => w.WarehouseTracking)
			);
			// Retrieve warehouse tracking detail by id
			var existingEntity = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết theo dõi kho"));
			}
			else
			{
				// Check for deleting detail of completed or cancelled warehouse tracking 
				if (existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Completed ||
				    existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Cancelled)
				{
					// Msg: Cannot change data as warehouse tracking was completed or cancelled
					return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
						await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
				}
			}
			
			// Check whether having constraints with other data
			if (existingEntity.LibraryItemId != null)
			{
				// Cannot process delete as warehouse tracking detail still contains item
				return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0010,
					await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0010));
			}
			
			// Process add delete entity
			await _unitOfWork.Repository<WarehouseTrackingDetail, int>().DeleteAsync(id);
			// Save to DB
			if (await _unitOfWork.SaveChangesAsync() > 0)
			{
				// Delete successfully
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
		    throw new Exception("Error invoke when process delete warehouse tracking detail");
	    }
    }

    public async Task<IServiceResult> UpdateItemFromInternalAsync(int trackingDetailId, int libraryItemId)
    {
	    try
	    {
            // Retrieve by id
            var existingEntity =
	            await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetByIdAsync(trackingDetailId);
            if (existingEntity == null)
            {
	            // Logging
	            _logger.Warning("Fail to update warehouse tracking detail due to entity not found");
	            
	            // Fail to update
	            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
            
            // Check exist library item
            var libraryItemDto = (await _itemSvc.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
            if (libraryItemDto == null)
            {
	            // Logging
	            _logger.Warning("Fail to update warehouse tracking detail due to library item not found");
	            
	            // Fail to update
	            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
			
            // Check for item change
            if (!Equals(existingEntity.LibraryItemId, libraryItemId))
            {
	            // Check whether update already exist item
	            if (existingEntity.LibraryItemId != null)
	            {
		            // Logging
		            _logger.Warning("Fail to update, as existing library item id change in warehouse tracking detail");
					
		            // Fail to update
		            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	            }
	            
	            // Update category must be the same with item
	            if (existingEntity.CategoryId != libraryItemDto.CategoryId)
	            {
		            // Fail to update
		            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	            }
	            
	            // Process update item
	            existingEntity.LibraryItemId = libraryItemId;
            }
			
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<WarehouseTrackingDetail, int>().HasChanges(existingEntity))
            {
	            // Update success
	            return new ServiceResult(ResultCodeConst.SYS_Success0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Perform update 
            await _unitOfWork.Repository<WarehouseTrackingDetail, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
	            // Update success
	            return new ServiceResult(ResultCodeConst.SYS_Success0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update item for warehouse tracking detail (internal procedure)");
	    }
    }

    public async Task<IServiceResult> UpdateItemFromExternalAsync(int trackingDetailId, int libraryItemId)
    {
	    try
	    {
			// Determine current system lang
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
		    // Build specification
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x =>
			    x.TrackingDetailId == trackingDetailId);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.WarehouseTracking)
		    );
		    
		    // Retrieve with spec
            var existingEntity =
                await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		            StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết dữ liệu nhập kho"));
            }
            else
            {
				// Check for updating detail of completed or cancelled warehouse tracking 
				if (existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Completed ||
				    existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Cancelled)
				{
					// Msg: Cannot change data as warehouse tracking was completed or cancelled
					return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
						await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
				}
            }
            
            // Check exist library item
            var libraryItemDto = (await _itemSvc.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
            if (libraryItemDto == null)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"), false);
            }
            
            // Check for item change
            if (!Equals(existingEntity.LibraryItemId, libraryItemId))
            {
	            // Check whether update already exist item
	            if (existingEntity.LibraryItemId != null)
	            {
		            // Fail to update
		            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003) + 
			            (isEng 
				            ? " as item already exist in warehouse tracking detail" 
				            : " vì tài liệu đã tồn tại cho chi tiết dữ liệu nhập kho"), false);
	            }
	            
	            // Check match ISBN (only process when item include ISBN)
	            if (!string.IsNullOrEmpty(libraryItemDto.Isbn) && !Equals(existingEntity.Isbn, libraryItemDto.Isbn))
	            {
		            // ISBN of selected warehouse tracking detail doesn't match
		            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0007,
			            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0007));
	            }
                
	            // Check whether warehouse tracking detail exist ISBN, but not for cataloging item  
	            if (string.IsNullOrEmpty(libraryItemDto.Isbn) && !string.IsNullOrEmpty(existingEntity.Isbn))
	            {
		            // Selected warehouse tracking detail is incorrect, cataloging item need ISBN to continue
		            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0008,
			            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0008));
	            }
	            
	            // Update category must be the same with item
	            if (existingEntity.CategoryId != libraryItemDto.CategoryId)
	            {
		            // Msg: The action cannot be performed as category of item and warehouse tracking detail is different
		            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0011,
			            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0011));
	            }
	            
	            // Process update item
	            existingEntity.LibraryItemId = libraryItemId;
            }
			
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<WarehouseTrackingDetail, int>().HasChanges(existingEntity))
            {
	            // Update success
	            return new ServiceResult(ResultCodeConst.SYS_Success0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Perform update 
            await _unitOfWork.Repository<WarehouseTrackingDetail, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
	            // Update success
	            return new ServiceResult(ResultCodeConst.SYS_Success0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update item for warehouse tracking detail (external procedure)");
	    }
    }
	
    public async Task<IServiceResult> AddToWarehouseTrackingAsync(int trackingId, WarehouseTrackingDetailDto dto)
    {
	    try
	    {
			// Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
            
		    // Check existing tracking id 
		    var trackingEntity = (await _trackingSvc.GetByIdAsync(trackingId)).Data as WarehouseTrackingDto;
		    if (trackingEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
		    }
		    else
		    {
			    // Check for change detail of completed or cancelled warehouse tracking 
			    if (trackingEntity.Status == WarehouseTrackingStatus.Completed ||
			        trackingEntity.Status == WarehouseTrackingStatus.Cancelled)
			    {
				    // Msg: Cannot change data as warehouse tracking was completed or cancelled
				    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
					    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
			    }
		    }
			
		    // Try to validate warehouse tracking detail dto
		    var validationResult = await ValidatorExtensions.ValidateAsync(dto);
		    // Check for valid validations
		    if (validationResult != null && !validationResult.IsValid)
		    {
			    // Convert ValidationResult to ValidationProblemsDetails.Errors
			    var errors = validationResult.ToProblemDetails().Errors;
			    throw new UnprocessableEntityException("Invalid Validations", errors);
		    }
		    
		    // Custom errors
		    var customErrors = new Dictionary<string, string[]>();
		    // Check exist ISBN
		    if(!string.IsNullOrEmpty(dto.Isbn) && 
		       await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(w => 
			       Equals(dto.Isbn, w.Isbn)))
		    {
			    customErrors.Add(
				    StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
				    [isEng 
					    ? $"ISBN '{dto.Isbn}' already exist in warehouse tracking detail" 
					    : $"Mã ISBN '{dto.Isbn}' đã tồn tại trong chi tiết theo dõi kho"
				    ]);
		    }
		    
		    // Check whether invoke any error
		    if(customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);
		    
		    // Add tracking id to dto 
		    dto.TrackingId = trackingId;
		    
		    // Progress add tracking detail to tracking 
		    await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AddAsync(_mapper.Map<WarehouseTrackingDetail>(dto));
		    // Save to DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    // Save success
			    return new ServiceResult(ResultCodeConst.SYS_Success0001,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
		    }
		    
		    // Fail to save
		    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process add warehouse tracking detail to tracking");
	    }
    }

    public async Task<IServiceResult> DeleteItemAsync(int trackingDetailId, int libraryItemId)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
			
		    // Build specification
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x =>
			    x.TrackingDetailId == trackingDetailId);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.WarehouseTracking)
		    );
		    
		    // Retrieve with spec
		    var existingEntity =
			    await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
		    if (existingEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết dữ liệu nhập kho"));
		    }
		    else
		    {
			    // Check for updating detail of completed or cancelled warehouse tracking 
			    if (existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Completed ||
			        existingEntity.WarehouseTracking.Status == WarehouseTrackingStatus.Cancelled)
			    {
				    // Msg: Cannot change data as warehouse tracking was completed or cancelled
				    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
					    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
			    }
		    }
            
		    // Check exist library item
		    var libraryItemDto = (await _itemSvc.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
		    if (libraryItemDto == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"));
		    }
		    
		    // Check whether requested item match with existing item in warehouse tracking detail
		    if (existingEntity.LibraryItemId != libraryItemId)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng 
					    ? "requested item in warehouse tracking detail" 
					    : "tài liệu trong chi tiết dữ liệu nhập kho"));
		    }
			
		    // Progress remove item from warehouse tracking detail
		    existingEntity.LibraryItemId = null;
		    
		    // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Delete success
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
            	    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
            }
            
            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process remove warehouse tracking detail from tracking");
	    }
    }
    
    public async Task<IServiceResult> GetAllByTrackingIdAsync(int trackingId)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
            
		    // Check existing tracking id 
		    var isTrackingExist = (await _trackingSvc.AnyAsync(x => x.TrackingId == trackingId)).Data is true;
		    if (!isTrackingExist)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
		    }
			
		    // Build warehouse tracking detail specification
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(w => w.TrackingId == trackingId);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.LibraryItem!) 
		    );
		    // Try to retrieve all data by spec
		    var entities = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    if (entities.Any())
		    {
			    // Get data successfully
			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				    _mapper.Map<List<WarehouseTrackingDetailDto>>(entities));
		    }

		    // Not found data or empty
		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
			    _mapper.Map<List<WarehouseTrackingDetailDto>>(entities));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all warehouse details");
	    }
    }

    public async Task<IServiceResult> GetAllNotExistItemByTrackingIdAsync(int trackingId)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Check existing tracking id 
		    var isTrackingExist = (await _trackingSvc.AnyAsync(x => x.TrackingId == trackingId)).Data is true;
		    if (!isTrackingExist)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
		    }
		    
		    // Build warehouse tracking detail specification
            var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(w => 
	            w.TrackingId == trackingId &&
	            w.LibraryItemId == null); // Exclude all detail containing library item
			
            // Try to retrieve all data by spec
            var entities = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
            	    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            	    _mapper.Map<List<WarehouseTrackingDetailDto>>(entities));
            }

            // Not found data or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                _mapper.Map<List<WarehouseTrackingDetailDto>>(entities));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all warehouse details");
	    }
    }
    
    public async Task<IServiceResult> ImportAsync(
        int trackingId, IFormFile file, string[]? scanningFields, DuplicateHandle? duplicateHandle)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check existing tracking id 
            var trackingEntity = (await _trackingSvc.GetByIdAsync(trackingId)).Data as WarehouseTrackingDto;
            if (trackingEntity == null)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		            StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
            }
            else
            {
	            // Check for change detail of completed or cancelled warehouse tracking 
	            if (trackingEntity.Status == WarehouseTrackingStatus.Completed ||
	                trackingEntity.Status == WarehouseTrackingStatus.Cancelled)
	            {
		            // Msg: Cannot change data as warehouse tracking was completed or cancelled
		            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
			            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
	            }
            }
            
            // Check exist file
            if (file.Length == 0)
            {
                return new ServiceResult(ResultCodeConst.File_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
            }

            // Validate import file 
            var validationResult = await ValidatorExtensions.ValidateAsync(file);
            if (validationResult != null && !validationResult.IsValid)
            {
                // Response the uploaded file is not supported
                throw new NotSupportedException(
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
                CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<WarehouseTrackingDetailCsvRecord>(
                    file, csvConfig, null, lang);
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
            
            // Try to detect wrong data
            var wrongDataErrs = await DetectWrongDataAsync(readResp.Records, lang);
            if (wrongDataErrs.Any())
            {
                foreach (var err in wrongDataErrs)
                {
	                // Check exist err in dictionary
	                if (readResp.Errors.ContainsKey(err.Key)) // already exist
	                {
		                readResp.Errors[err.Key] = readResp.Errors[err.Key]
			                .Concat(err.Value.ToArray()).ToArray();
	                }
	                else // not exist
	                {
		                readResp.Errors.Add(err.Key, err.Value.ToArray());
	                }
                }
                
                var errorResps = readResp.Errors.Select(x => new ImportErrorResultDto()
                {	
	                RowNumber = x.Key,
	                Errors = x.Value.ToList()
                });
                
                return new ServiceResult(ResultCodeConst.SYS_Fail0008,
	                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
            }
            
            // Additional msg 
            var additionalMsg = string.Empty;
            // Detect duplicates
            var detectDuplicateResult = DetectDuplicatesInFile(readResp.Records, scanningFields ?? [], lang);
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
            
            var categories = (await _cateSvc.GetAllAsync()).Data as List<CategoryDto>;
            if (categories == null || !categories.Any())
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	                StringUtils.Format(msg, isEng
		                ? "categories to process import"
		                : "phân loại để tiến hành import"));
            }
            
            // Initialize list of warehouse tracking detail
            var warehouseTrackingDetails = new List<WarehouseTrackingDetailDto>();
            // Progress import warehouse tracking detail
            foreach (var record in readResp.Records)
            {
                // Assign category id
                var category = categories.First(c =>
	                Equals(c.EnglishName.ToLower(), record.Category.ToLower()) ||
	                Equals(c.VietnameseName.ToLower(), record.Category.ToLower()));
                // Convert to dto detail
                var trackingDetailDto = record.ToWarehouseTrackingDetailDto();
                // Assign category id
                trackingDetailDto.CategoryId = category.CategoryId;
                // Assign tracking id 
                trackingDetailDto.TrackingId = trackingId;
                // Add to warehouse tracking
                warehouseTrackingDetails.Add(trackingDetailDto);
            }

            if (warehouseTrackingDetails.Any())
            {
	            // Progress add range
	            await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AddRangeAsync(
		            _mapper.Map<List<WarehouseTrackingDetail>>(warehouseTrackingDetails));
	            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
	            if (isSaved)
	            {
		            var respMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
		            respMsg = !string.IsNullOrEmpty(additionalMsg)
			            ? $"{StringUtils.Format(respMsg, warehouseTrackingDetails.Count.ToString())}, {additionalMsg}"
			            : StringUtils.Format(respMsg, warehouseTrackingDetails.Count.ToString());
		            return new ServiceResult(ResultCodeConst.SYS_Success0005, respMsg, true);
	            }
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0005,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0005), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process warehouse tracking detail import");
        }
    }
    
    private async Task<Dictionary<int, List<string>>> DetectWrongDataAsync(
    	    List<WarehouseTrackingDetailCsvRecord> records,
    	    SystemLanguage? lang)
        {
    	    // Determine current system language
    	    var isEng = lang == SystemLanguage.English;
            
    	    // Initialize error messages (for display purpose)
    	    var errorMessages = new Dictionary<int, List<string>>();
	        // Initialize isbn hashset to check duplicates
	        var isbnHashSet = new HashSet<string>();
			
    	    // Default row index set to second row, as first row is header
    	    var currDataRow = 2;
    	    // Check exist category for each record
    	    for (int i = 0; i < records.Count; i++)
    	    {
    		    var record = records[i];
    	                
    		    // Initialize row errors
    		    var rowErrors = new List<string>();
    			
    		    // Check exist category
    		    if ((await _cateSvc.AnyAsync(c =>
    			        Equals(c.EnglishName.ToLower(), record.Category.ToLower()) || 
    			        Equals(c.VietnameseName.ToLower(), record.Category.ToLower())
    		        )).Data is false)
    		    {
    			    rowErrors.Add(isEng ? "Category name not exist" : "Tên phân loại không tồn tại");
    		    }
    		    
		        var cleanedIsbn = ISBN.CleanIsbn(record.Isbn ?? string.Empty);
		        if (!string.IsNullOrEmpty(cleanedIsbn)) // Check empty ISBN
		        {
			        // Validate ISBN 
			        if(cleanedIsbn.Length > 13 || !ISBN.IsValid(cleanedIsbn, out _))
			        {
				        rowErrors.Add(isEng ? $"ISBN '{record.Isbn}' is not valid" : $"Mã ISBN '{record.Isbn}' không hợp lệ");
			        }
			        // Check exist ISBN
			        if(await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(w => 
				           Equals(cleanedIsbn, w.Isbn)))
			        {
				        rowErrors.Add(isEng 
					        ? $"ISBN '{record.Isbn}' already exist in warehouse tracking detail" 
					        : $"Mã ISBN '{record.Isbn}' đã tồn tại trong chi tiết theo dõi kho");
			        }
			        // Check uniqueness
			        if (!isbnHashSet.Add(cleanedIsbn))
			        {
				        rowErrors.Add(isEng
					        ? $"ISBN '{record.Isbn}' is duplicated"
					        : $"ISBN '{record.Isbn}' đã bị trùng");
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
    
    	    return errorMessages;
        }
        
    private (Dictionary<int, List<string>> Errors, Dictionary<int, List<int>> Duplicates) DetectDuplicatesInFile(
    	List<WarehouseTrackingDetailCsvRecord> records,
    	string[] scanningFields,
    	SystemLanguage? lang
    )
    {
    	// Check whether exist any scanning fields
    	if (scanningFields.Length == 0)
    		return (new(), new());
    	
    	// Determine current system language
    	var isEng = lang == SystemLanguage.English;
        
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
    				var name when name == nameof(WarehouseTrackingDetail.ItemName).ToUpperInvariant() => record.ItemName?.Trim()
    					.ToUpperInvariant(),
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

    	return (errorMessages, duplicates);
    }
}