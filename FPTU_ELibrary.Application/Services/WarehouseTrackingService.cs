using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Cloudinary;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Dtos.Suppliers;
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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class WarehouseTrackingService : GenericService<WarehouseTracking, WarehouseTrackingDto, int>, 
    IWarehouseTrackingService<WarehouseTrackingDto>
{
	// App settings
    private readonly AppSettings _appSettings;
    
    // Lazy services
    private readonly Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> _trackingDetailService;
    private readonly Lazy<ISupplementRequestDetailService<SupplementRequestDetailDto>> _supplementDetailService;
    
    // Normal services
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ICategoryService<CategoryDto> _cateService;
    private readonly ICloudinaryService _cloudService;
    private readonly ISupplierService<SupplierDto> _supplierService;
    private readonly ILibraryItemService<LibraryItemDto> _itemService;
    private readonly ILibraryShelfService<LibraryShelfDto> _shelfService;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceService;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionService;

    public WarehouseTrackingService(
	    // Lazy services
	    Lazy<IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>> trackingDetailService,
	    Lazy<ISupplementRequestDetailService<SupplementRequestDetailDto>> supplementDetailService,
	    
	    // Normal services
	    IAuthorService<AuthorDto> authorService,
        ICategoryService<CategoryDto> cateService,
        ISupplierService<SupplierDto> supplierService,
        ILibraryItemService<LibraryItemDto> itemService,
	    ILibraryShelfService<LibraryShelfDto> shelfService,
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceService,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionService,
        IOptionsMonitor<AppSettings> monitor,
	    ICloudinaryService cloudService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _appSettings = monitor.CurrentValue;
	    _authorService = authorService;
	    _cateService = cateService;
        _itemService = itemService;
	    _cloudService = cloudService;
	    _shelfService = shelfService;
	    _supplierService = supplierService;
        _conditionService = conditionService;
        _itemInstanceService = itemInstanceService;
        _trackingDetailService = trackingDetailService;
        _supplementDetailService = supplementDetailService;
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
	    try
	    {
			// Build specification
			var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == id);
			// Apply including supplier
			baseSpec.ApplyInclude(q => q
				.Include(w => w.Supplier)
				.Include(w => w.WarehouseTrackingInventory)
			);
			// Retrieve entity by id
			var entity = await _unitOfWork.Repository<WarehouseTracking, int>().GetWithSpecAsync(baseSpec);
			if (entity != null)
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<WarehouseTrackingDto>(entity));
			}
			
			// Response as data not found or empty
			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
	    }
	    catch (Exception ex)
	    {
		    throw new Exception("Error invoke when process get by warehouse tracking id");
	    }
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<WarehouseTracking> specification, bool tracked = true)
    {
	    try
	    {
		    // Try to parse specification to WarehouseTrackingSpecification
		    var trackingSpec = specification as WarehouseTrackingSpecification;
		    // Check if specification is null
		    if (trackingSpec == null)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }	
		    
		    // Count total warehouse tracking
		    var totalTrackingWithSpec = await _unitOfWork.Repository<WarehouseTracking, int>().CountAsync(trackingSpec);
		    // Count total page
		    var totalPage = (int)Math.Ceiling((double)totalTrackingWithSpec / trackingSpec.PageSize);
				
		    // Set pagination to specification after count total warehouse tracking 
		    if (trackingSpec.PageIndex > totalPage 
		        || trackingSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
		    {
			    trackingSpec.PageIndex = 1; // Set default to first page
		    }
				
		    // Apply pagination
		    trackingSpec.ApplyPaging(
			    skip: trackingSpec.PageSize * (trackingSpec.PageIndex - 1), 
			    take: trackingSpec.PageSize);
		    
		    // Get all with spec
		    var entities = await _unitOfWork.Repository<WarehouseTracking, int>()
			    .GetAllWithSpecAsync(trackingSpec, tracked);
		    if (entities.Any()) // Exist data
		    {
			    // Convert to dto collection 
			    var trackingDtos = _mapper.Map<List<WarehouseTrackingDto>>(entities);
					
			    // Pagination result 
			    var paginationResultDto = new PaginatedResultDto<WarehouseTrackingDto>(trackingDtos,
				    trackingSpec.PageIndex, trackingSpec.PageSize, totalPage, totalTrackingWithSpec);
					
			    // Response with pagination 
			    return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
		    }
				
		    // Not found any data
		    return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
			    _mapper.Map<List<WarehouseTrackingDto>>(entities));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all warehouse tracking");
	    }
    }
	
    public override async Task<IServiceResult> UpdateAsync(int id, WarehouseTrackingDto dto)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;

		    // Check exist entity 
		    var existingEntity = await _unitOfWork.Repository<WarehouseTracking, int>().GetByIdAsync(id);
		    if (existingEntity == null)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(msg, isEng ? "warehouse tracking" : "thông tin theo dõi do"));
		    }

		    if (await _unitOfWork.Repository<WarehouseTracking, int>()
			        .AnyAsync(w =>
				        w.WarehouseTrackingDetails.Any(wd => wd.LibraryItemId != null)))
		    {
			    // Cannot process update as exist item has been cataloged
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0012,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0012));
		    }

		    // Initialize custom errors
		    var customErrors = new Dictionary<string, string[]>();

		    // Check exist supplier
		    var isSupplierExist = (await _supplierService.AnyAsync(s => s.SupplierId == dto.SupplierId)).Data is true;
		    if (!isSupplierExist)
		    {
			    // Not found {0}
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
				    StringUtils.Format(errMsg, isEng 
					    ? "supplier" 
					    : "nhà phân phối hoặc nhà cung cấp"));
		    }
		    
		    // Check whether invoke any errors
		    if (customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);

		    // Check status
		    if (existingEntity.Status != WarehouseTrackingStatus.Draft)
		    {
			    // Msg: Not allow to perform update when status is not Draft
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0002));
		    }

		    // Check whether exist any cataloged item
		    if (await _unitOfWork.Repository<WarehouseTracking, int>().AnyAsync(w =>
			        w.TrackingId == id &&
			        w.WarehouseTrackingDetails.Any(wd => wd.LibraryItemId != null)))
		    {
			    // Msg: Cannot change data as existing item has been cataloged
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0014,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0014));
		    }
		    
		    // Progress update properties
		    existingEntity.SupplierId = dto.SupplierId;
		    existingEntity.TotalItem = dto.TotalItem;
		    existingEntity.TotalAmount = dto.TotalAmount;
		    existingEntity.TrackingType = dto.TrackingType;
		    existingEntity.TransferLocation = dto.TransferLocation;
		    existingEntity.Description = dto.Description;
		    existingEntity.EntryDate = dto.EntryDate;
		    existingEntity.ExpectedReturnDate = dto.ExpectedReturnDate;

		    // Progress update to DB
		    await _unitOfWork.Repository<WarehouseTracking, int>().UpdateAsync(existingEntity);
		    // Save DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Success0003,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
		    }

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
		    throw new Exception("Error invoke when process update warehouse tracking");
	    }
    }

    public async Task<IServiceResult> GetAllStockTransactionTypeByTrackingTypeAsync(
	    TrackingType trackingType)
    {
	    try
	    {
			// Initialize list of tracking type
			List<StockTransactionType> transactionTypes = null;
			
			// Determine tracking type
			switch (trackingType)
			{
				case TrackingType.StockIn:
					transactionTypes = new ()
					{
						StockTransactionType.New,
						StockTransactionType.Additional
					};
					break;
				case TrackingType.StockOut:
					transactionTypes = new ()
					{
						StockTransactionType.Damaged,
						StockTransactionType.Lost,
						StockTransactionType.Outdated,
						StockTransactionType.Other
					};
					break;
				case TrackingType.StockChecking:
					transactionTypes = new ()
					{
						StockTransactionType.New,
						StockTransactionType.Damaged,
						StockTransactionType.Lost,
						StockTransactionType.Outdated,
						StockTransactionType.Other,
					};
					break;
				case TrackingType.SupplementRequest:
					transactionTypes = new ()
					{
						StockTransactionType.Reorder
					};
					break;
			}
			
			// Get data successfully
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), transactionTypes);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all stock transaction type by name");
	    }
    }

    public async Task<IServiceResult> GetByIdAndIncludeInventoryAsync(int trackingId)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == trackingId);
		    // Apply include
		    baseSpec.ApplyInclude(q => q.Include(w => w.WarehouseTrackingInventory));
			// Retrieve data with spec
		    var existingEntity = await _unitOfWork.Repository<WarehouseTracking, int>()
				.GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				// Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
					StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin nhập/xuất kho"));
			}
			
			// Get data successfully
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<WarehouseTrackingDto>(existingEntity));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get warehouse tracking by id and include its inventory");
	    }
    }
    
    public async Task<IServiceResult> UpdateStatusAsync(int id, WarehouseTrackingStatus status)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;

		    // Build specification
		    var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == id);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.WarehouseTrackingDetails)
					.ThenInclude(wd => wd.LibraryItem)
						.ThenInclude(li => li!.LibraryItemInventory)
		    );
		    // Check exist warehouse tracking 
			var existingEntity = await _unitOfWork.Repository<WarehouseTracking, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(msg, isEng ? "warehouse tracking" : "thông tin theo dõi do"));
			}
			
			// Mark as not allow to change
			var isNotAllowToChange = false;
			// Check whether status change
			if (!Equals(existingEntity.Status, status))
			{
				// Progress update status 
				// Case 1: Draft -> Completed
				// Case 2: Draft -> Cancelled
				// Case 3: Completed -> Draft
				// Case 4: Completed -> Cancelled (not allow)
				// Case 5: Cancelled -> Draft (not allow)
				// Case 6: Cancelled -> Completed (not allow)

				switch (existingEntity.Status)
				{
					case WarehouseTrackingStatus.Draft:
						// Case 1: Draft -> Completed
						if (status == WarehouseTrackingStatus.Completed)
						{
							if (existingEntity.WarehouseTrackingDetails.Any(wd => wd.LibraryItemId == null))
							{
								// Cannot change status to completed, as existing item has not been cataloged yet
								return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0004,
									await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0004));
							}
							else
							{
								// Check whether total library item's instances equals with actual warehouse tracking total
								foreach (var detail in existingEntity.WarehouseTrackingDetails)
								{
									if (detail.LibraryItem!.LibraryItemInventory.TotalUnits < detail.ItemTotal)
									{
										// Cannot change status to completed, as total item instance is not enough
										// compared to the total of warehouse tracking information
										return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0005,
											await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0005));
									}
								}
							}
							
							// Change status to completed
							existingEntity.Status = WarehouseTrackingStatus.Completed;
						}
						// Case 2: Draft -> Cancelled
						else if (status == WarehouseTrackingStatus.Cancelled)
						{
							existingEntity.Status = WarehouseTrackingStatus.Cancelled;
						}
						
						break;
					case WarehouseTrackingStatus.Completed:
						// Case 3: Completed -> Draft
						if (status == WarehouseTrackingStatus.Draft)
						{
							existingEntity.Status = WarehouseTrackingStatus.Draft;
						}
						// Case 4: Completed -> Cancelled (not allow)
						else isNotAllowToChange = true;
						
						break;
					case WarehouseTrackingStatus.Cancelled:
						// Case 5: Cancelled -> Draft (not allow)
						// Case 6: Cancelled -> Completed (not allow)
						isNotAllowToChange = true;
						break;
				}
			}
			else
			{
				// Return success, as nothing change
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
			}
			
			// Check whether is allow to change
			if (isNotAllowToChange)
			{
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0001);
				return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0001,
					StringUtils.Format(msg, existingEntity.Status.ToString(), status.ToString()));
			}
			
			// Progress update to DB
			await _unitOfWork.Repository<WarehouseTracking, int>().UpdateAsync(existingEntity);
			// Save DB
			var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
			if (isSaved)
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
			}

			return new ServiceResult(ResultCodeConst.SYS_Fail0003,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke while process update warehouse tracking status");
	    }
    }

    public async Task<IServiceResult> UpdateInventoryWithoutSaveChanges(int id, WarehouseTrackingDto dto)
    {
	    try
	    {
		    // Build spec
		    var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == id);
		    // Apply include
		    baseSpec.ApplyInclude(q => q.Include(w => w.WarehouseTrackingInventory));
			// Retrieve existing
			var existingEntity = await _unitOfWork.Repository<WarehouseTracking, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				// Mark as fail to update
				return new ServiceResult(ResultCodeConst.SYS_Fail0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
			}
			
			// Process update inventory props
			existingEntity.WarehouseTrackingInventory.TotalItem = dto.WarehouseTrackingInventory.TotalItem;
			existingEntity.WarehouseTrackingInventory.TotalInstanceItem = dto.WarehouseTrackingInventory.TotalInstanceItem;
			existingEntity.WarehouseTrackingInventory.TotalCatalogedItem = dto.WarehouseTrackingInventory.TotalCatalogedItem;
			existingEntity.WarehouseTrackingInventory.TotalCatalogedInstanceItem = dto.WarehouseTrackingInventory.TotalCatalogedInstanceItem;
			
			// Mark as success to update
			return new ServiceResult(ResultCodeConst.SYS_Success0003,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update warehouse tracking inventory without saving");
	    }
    }
    
    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build specification
		    var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == id);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    .Include(w => w.WarehouseTrackingDetails)
		    );
		    // Check exist entity 
		    var existingEntity = await _unitOfWork.Repository<WarehouseTracking, int>().GetWithSpecAsync(baseSpec);
		    if (existingEntity == null)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(msg, isEng ? "warehouse tracking" : "thông tin theo dõi do"));
		    }
		    
		    // Check status
		    if (existingEntity.Status != WarehouseTrackingStatus.Draft)
		    {
			    // Not allow to perform update when status is not Draft
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0002));
		    }
		    
		    // Check constraints
		    if (existingEntity.WarehouseTrackingDetails.Any(wd => wd.LibraryItemId != null))
		    {
			    // Cannot delete warehouse tracking information, as existing item has been cataloged
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0003,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0003));
		    }
		    
		    // Progress delete 
		    await _unitOfWork.Repository<WarehouseTracking, int>().DeleteAsync(id);
		    // Save DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Success0004,
                	await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
		    }
		    
		    // Fail to save
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
		    throw new Exception("Error invoke when process delete warehouse tracking");
	    }
    }

    public async Task<IServiceResult> CreateSupplementRequestASync(WarehouseTrackingDto dto)
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
			    throw new UnprocessableEntityException("Invalid Validations", errors);
		    }
			
		    // Initialize custom errors
            var customErrors = new Dictionary<string, string[]>();
            // Initialize unique isbn check
            var uniqueIsbnSet = new HashSet<string>();
            // Iterate each warehouse tracking detail (if any) to validate data
		    var wTrackingDetailList = dto.WarehouseTrackingDetails.ToList();
		    for (int i = 0; i < wTrackingDetailList.Count; ++i)
		    {
			    var wDetail = wTrackingDetailList[i];
			    
			    // Check exist supplement request reason
			    if (string.IsNullOrEmpty(wDetail.SupplementRequestReason))
			    {
				    // Add error
				    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					    key: $"warehouseTrackingDetails[{i}].supplementRequestReason",
					    msg: isEng ? "Supplement request reason is required" : "Lý do yêu cầu bổ sung không được rỗng");
			    }
			    
			    // Check exist library item
			    if (!int.TryParse(wDetail.LibraryItemId.ToString(), out var validItemId))
			    {
				    // Add error
				    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					    key: $"warehouseTrackingDetails[{i}].libraryItemId",
						// Msg: Supplement request item not found
					    msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0022));
			    }
			    // Retrieve item by id
			    else if((await _itemService.GetByIdAsync(validItemId)).Data is LibraryItemDto libItem)
			    {
				    // Validate warehouse tracking details
				    validationResult = await ValidatorExtensions.ValidateAsync(wDetail);
				    // Check for valid validations
				    if (validationResult != null && !validationResult.IsValid)
				    {
					    // Convert ValidationResult to ValidationProblemsDetails.Errors
					    var errors = validationResult.ToProblemDetails().Errors;
					    
					    // Initialize err properties
					    var itemNameErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ItemName));
					    var isbnErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn));
					    var unitPriceErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.UnitPrice));
					    var totalAmountErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TotalAmount));
					    var itemTotalErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ItemTotal));
					    
					    // Map error
					    if (errors.TryGetValue(itemNameErrKey, out var itemNameErrs)) // Item name
					    {
						    // Add error
						    foreach (var errMsg in itemNameErrs)
						    {
							    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
								    key: $"warehouseTrackingDetail[{i}].{itemNameErrKey}",
								    msg: errMsg);
						    }
					    }
						if (errors.TryGetValue(isbnErrKey, out var isbnErrs)) // Isbn
                        {
                            // Add error
                            foreach (var errMsg in isbnErrs)
                            {
                        	    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                        		    key: $"warehouseTrackingDetail[{i}].{isbnErrKey}",
                        		    msg: errMsg);
                            }
                        }
                        if (errors.TryGetValue(unitPriceErrKey, out var unitPriceErrs)) // Unit price
                        {
	                        // Add error
	                        foreach (var errMsg in unitPriceErrs)
	                        {
		                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                        key: $"warehouseTrackingDetail[{i}].{unitPriceErrKey}",
			                        msg: errMsg);
	                        }
                        }
                        if (errors.TryGetValue(totalAmountErrKey, out var totalAmountErrs)) // Total amount
                        {
	                        // Add error
	                        foreach (var errMsg in totalAmountErrs)
	                        {
		                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                        key: $"warehouseTrackingDetail[{i}].{totalAmountErrKey}",
			                        msg: errMsg);
	                        }
                        }
                        if (errors.TryGetValue(itemTotalErrKey, out var itemTotalErrs)) // Item total
                        {
	                        // Add error
	                        foreach (var errMsg in itemTotalErrs)
	                        {
		                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                        key: $"warehouseTrackingDetail[{i}].{itemTotalErrKey}",
			                        msg: errMsg);
	                        }
                        }
				    }
				    
				    // Check isbn uniqueness
				    if (string.IsNullOrEmpty(wDetail.Isbn) || !uniqueIsbnSet.Add(wDetail.Isbn))
				    {
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"warehouseTrackingDetails[{i}].isbn",
						    msg: isEng ? $"ISBN '{wDetail.Isbn}' is duplicated" : $"Mã ISBN '{wDetail.Isbn}' đã bị trùng");
				    }
				    
				    // Compare ISBN match
					if (!Equals(libItem.Isbn, wDetail.Isbn))
					{
						// Add error 
						customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							key: $"warehouseTrackingDetails[{i}].isbn",
							// Msg: ISBN doesn't match with supplement request item
							msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0023));
					}
					
				    // Check exist category
				    if ((await _cateService.GetByIdAsync(wDetail.CategoryId)).Data is null)
				    {
					    // msg: Not found {0}
					    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"warehouseTrackingDetails[{i}].categoryId",
						    msg: StringUtils.Format(errMsg, isEng ? "item category" : "phân loại tài liệu"));
				    }
				    
				    // Compare category match
				    if (!Equals(libItem.CategoryId, wDetail.CategoryId))
				    {
					    // Add error 
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"warehouseTrackingDetails[{i}].categoryId",
						    msg: isEng 
							    ? "Category doesn't match with supplement request item"
							    : "Phân loại của tài liệu không trùng với tài liệu yêu cầu bổ sung");
				    }
				    
				    // Check exist condition
				    if ((await _conditionService.GetByIdAsync(wDetail.ConditionId)).Data is null)
				    {
					    // Msg: Not found {0}
					    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"warehouseTrackingDetails[{i}].conditionId",
						    msg: StringUtils.Format(errMsg, isEng ? "item condition" : "tình trạng tài liệu hiện tại"));
				    }
				    
				    // Validate stock transaction type
				    if (wDetail.StockTransactionType != StockTransactionType.Reorder)
				    {
					    // Msg: The stock transaction type {0} is invalid for creating a supplement request
					    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0024);
					    // Add error
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                            key: $"warehouseTrackingDetails[{i}].stockTransactionType",
                            msg: StringUtils.Format(errMsg, isEng ? wDetail.StockTransactionType.ToString() : wDetail.StockTransactionType.GetDescription()));
				    }
			    }
		    }

		    // Iterate each supplement requests (if any) to validate data
		    var supplementDetailList = dto.SupplementRequestDetails.ToList();
		    for (int i = 0; i < supplementDetailList.Count; ++i)
		    {
			    var supplementReqDetail = supplementDetailList[i];
			    
			    // Validate supplement request detail
			    validationResult = await ValidatorExtensions.ValidateAsync(supplementReqDetail);
			    // Check for valid validations
			    if (validationResult != null && !validationResult.IsValid)
			    {
				    // Convert ValidationResult to ValidationProblemsDetails.Errors
				    var errors = validationResult.ToProblemDetails().Errors;
				    
				    // Initialize err properties
				    var titleErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Title));
				    var authorErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Author));
				    var publisherErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Publisher));
				    var publishedDateErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.PublishedDate));
				    var descErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Description));
				    var isbnErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Isbn));
				    var pageCountErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.PageCount));
				    var dimensionErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Dimensions));
				    var categoryErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Categories));
				    var avgRatingErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.AverageRating));
				    var ratingCountErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.RatingsCount));
				    var langErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.Language));
				    var coverImageLinkErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.CoverImageLink));
				    var infoLinkLinkErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.InfoLink));
				    var previewLinkErrKey = StringUtils.ToCamelCase(nameof(SupplementRequestDetail.PreviewLink));
				    
				    // Initialize err msg
				    string[]? errMessages;
				    // Map error
				    if (errors.TryGetValue(titleErrKey, out errMessages)) // Title
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{titleErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(authorErrKey, out errMessages)) // Author
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{authorErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(publisherErrKey, out errMessages)) // Publisher
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{publisherErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(publishedDateErrKey, out errMessages)) // Published date
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{publishedDateErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(descErrKey, out errMessages)) // Description
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{descErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(isbnErrKey, out errMessages)) // ISBN
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{isbnErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(pageCountErrKey, out errMessages)) // Page count
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{pageCountErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(dimensionErrKey, out errMessages)) // Dimensions
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{dimensionErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(categoryErrKey, out errMessages)) // Categories
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{categoryErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(avgRatingErrKey, out errMessages)) // Average rating
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{avgRatingErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(ratingCountErrKey, out errMessages)) // Rating count
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{ratingCountErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(langErrKey, out errMessages)) // Language
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{langErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(coverImageLinkErrKey, out errMessages)) // Cover image
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{coverImageLinkErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(infoLinkLinkErrKey, out errMessages)) // Info link
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{infoLinkLinkErrKey}",
							    msg: errMsg);
					    }
				    }
				    if (errors.TryGetValue(previewLinkErrKey, out errMessages)) // Preview link
				    {
					    // Add error
					    foreach (var errMsg in errMessages)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"supplementRequestDetails[{i}].{previewLinkErrKey}",
							    msg: errMsg);
					    }
				    }
			    }
			    
			    // Check exist related item
			    if ((await _itemService.GetByIdAsync(supplementReqDetail.RelatedLibraryItemId)).Data is null)
			    {
				    // Add error 
				    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					    key: $"supplementRequestDetails[{i}].relatedLibraryItemId",
					    // Msg: No related item was found for supplement request item
					    msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0026));
			    }
			    
			    // Check exist ISBN	
			    if ((await _itemService.AnyAsync(li => 
				        li.Isbn != null &&
						li.Isbn.Equals(supplementReqDetail.Isbn))).Data is true)
			    {
				    // Add error 
				    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					    key: $"supplementRequestDetails[{i}].isbn",
					    // Msg: The ISBN for the item suggested for extra acquisition is already in use
					    msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0025));
			    }
		    }
			
		    // Check whether invoke any errors
		    if(customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);
		    
		    // Generate receipt number
            var receiptNum = $"PN{StringUtils.GenerateRandomCodeDigits(8)}";
            // Add necessary fields
            dto.ReceiptNumber = receiptNum;
            dto.TotalItem = (dto.WarehouseTrackingDetails?.Count ?? 0) + (dto.SupplementRequestDetails?.Count ?? 0);
            dto.TotalAmount = (dto.WarehouseTrackingDetails != null && dto.WarehouseTrackingDetails.Any() ? dto.WarehouseTrackingDetails.Sum(wtd => wtd.TotalAmount) : 0)
                              + (dto.SupplementRequestDetails != null && dto.SupplementRequestDetails.Any() ? dto.SupplementRequestDetails.Sum(srd => srd.EstimatedPrice ?? 0) : 0);
            dto.TrackingType = TrackingType.SupplementRequest;
            dto.Status = WarehouseTrackingStatus.Draft;
		    
            // Process add new warehouse tracking with details
            await _unitOfWork.Repository<WarehouseTracking, int>().AddAsync(_mapper.Map<WarehouseTracking>(dto));
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
	            // Msg: Supplement request created successfully
	            return new ServiceResult(ResultCodeConst.WarehouseTracking_Success0002,
		            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Success0002));
            }

            // Msg: Failed to create supplement request
            return new ServiceResult(ResultCodeConst.WarehouseTracking_Fail0002,
	            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Fail0002));
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process create supplement request");
	    }
    }
    
    public async Task<IServiceResult> CreateStockInWithDetailsAsync(WarehouseTrackingDto dto)
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
			    throw new UnprocessableEntityException("Invalid Validations", errors);
		    }

		    // Check exist supplier
		    var isSupplierExist = (await _supplierService.AnyAsync(s => s.SupplierId == dto.SupplierId)).Data is true;
		    if (!isSupplierExist)
		    {
			    // Not found {0}
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "supplier" : "nhà phân phối hoặc nhà cung cấp"));
		    }

		    // Initialize custom errors
		    var customErrors = new Dictionary<string, string[]>();
			// Initialize unique isbn check
			var uniqueIsbnSet = new HashSet<string>();
			// Initialize skip item dic 
			var skipItemDic = new Dictionary<int, int>(); // KeyPair (categoryId, totalSkipVal)
			// Initialize dictionary to accumulating skip item
			var accumulateDic = new Dictionary<int, int>();
			
		    // Iterate each warehouse tracking detail (if any) to validate data
		    var wTrackingDetailList = dto.WarehouseTrackingDetails.ToList();
		    for (int i = 0; i < wTrackingDetailList.Count; ++i)
		    {
			    var wDetail = wTrackingDetailList[i];
				
			    // Validate warehouse tracking details
			    validationResult = await ValidatorExtensions.ValidateAsync(wDetail);
			    // Check for valid validations
			    if (validationResult != null && !validationResult.IsValid)
			    {
				    // Convert ValidationResult to ValidationProblemsDetails.Errors
				    var errors = validationResult.ToProblemDetails().Errors;
				    
				    // Initialize err properties
				    var itemNameErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ItemName));
				    var isbnErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn));
				    var unitPriceErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.UnitPrice));
				    var totalAmountErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.TotalAmount));
				    var itemTotalErrKey = StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ItemTotal));
				    
				    // Map error
				    if (errors.TryGetValue(itemNameErrKey, out var itemNameErrs)) // Item name
				    {
					    // Add error
					    foreach (var errMsg in itemNameErrs)
					    {
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetail[{i}].{itemNameErrKey}",
							    msg: errMsg);
					    }
				    }
					if (errors.TryGetValue(isbnErrKey, out var isbnErrs)) // Isbn
                    {
                        // Add error
                        foreach (var errMsg in isbnErrs)
                        {
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                        	    key: $"warehouseTrackingDetail[{i}].{isbnErrKey}",
                        	    msg: errMsg);
                        }
                    }
                    if (errors.TryGetValue(unitPriceErrKey, out var unitPriceErrs)) // Unit price
                    {
                        // Add error
                        foreach (var errMsg in unitPriceErrs)
                        {
	                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
		                        key: $"warehouseTrackingDetail[{i}].{unitPriceErrKey}",
		                        msg: errMsg);
                        }
                    }
                    if (errors.TryGetValue(totalAmountErrKey, out var totalAmountErrs)) // Total amount
                    {
                        // Add error
                        foreach (var errMsg in totalAmountErrs)
                        {
	                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
		                        key: $"warehouseTrackingDetail[{i}].{totalAmountErrKey}",
		                        msg: errMsg);
                        }
                    }
                    if (errors.TryGetValue(itemTotalErrKey, out var itemTotalErrs)) // Item total
                    {
                        // Add error
                        foreach (var errMsg in itemTotalErrs)
                        {
	                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
		                        key: $"warehouseTrackingDetail[{i}].{itemTotalErrKey}",
		                        msg: errMsg);
                        }
                    }
			    }
			    
			    // Set default library item (if null or equals 0)
			    wDetail.LibraryItemId ??= 0;
			    // Check whether create warehouse tracking detail with item
			    if (wDetail.LibraryItemId > 0 && wDetail.LibraryItem != null)
			    {
				    // Mark as fail to create
				    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
					    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
			    }
			    
                // Retrieve skip item from dic with specific category id (this would ensure that all warehouse tracking have unique barcode)
                if (!skipItemDic.ContainsKey(wDetail.CategoryId))
                {
	                // Add default skip val
	                skipItemDic[wDetail.CategoryId] = 0;
	                // Add default accumulate val
	                accumulateDic[wDetail.CategoryId] = 0;
                }
                else
                {
	                // Increase skip val based on warehouse tracking item total
	                skipItemDic[wDetail.CategoryId] = accumulateDic[wDetail.CategoryId];
                }
                
                var skipVal = skipItemDic[wDetail.CategoryId];
                // Generate barcode range based on registered warehouse tracking detail
                var generateRes = await _itemInstanceService.GenerateBarcodeRangeAsync(
	                categoryId: wDetail.CategoryId,
	                totalItem: wDetail.ItemTotal,
	                skipItem: skipVal);
                var barcodeRangeRes = generateRes.Data as GenerateBarcodeRangeResultDto;
                
                if (barcodeRangeRes != null && barcodeRangeRes.Barcodes.Any())
                {
	                if (wDetail.LibraryItemId > 0 && wDetail.LibraryItem == null)
				    {
					    // Check transaction type
					    if (wDetail.StockTransactionType != StockTransactionType.Additional)
					    {
						    // Add error
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].stockTransactionType",
							    msg: isEng 
								    ? $"Transaction type must be '{StockTransactionType.Additional.ToString()}' when add tracking detail with item's catalog information" 
								    : $"Loại biến động kho cần chuyển sang '{StockTransactionType.Additional.GetDescription()}' khi tạo dữ liệu nhập kho đi kèm với thông tin biên mục");
					    }
					    else
					    {
						    // Check exist library item id 
						    var libItemDto = (await _itemService.GetByIdAsync(
							    int.Parse(wDetail.LibraryItemId.ToString() ?? "0"))).Data as LibraryItemDto;
						    if (libItemDto == null)
						    {
							    // Not found {0}
							    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
							    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
								    StringUtils.Format(errMsg, isEng 
									? "selected library item for warehouse tracking registered" 
									: "tài liệu đã chọn cho đăng ký tài liệu nhập kho"));
						    }
						    
						    // Check same category with warehouse tracking
						    if (!Equals(wDetail.CategoryId, libItemDto.CategoryId))
						    {
							    // Add errors
							    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
								    key: $"warehouseTrackingDetails[{i}].libraryItem.categoryId",
								    msg: isEng
									    ? "Category among warehouse tracking detail with catalog item must be the same"
									    : "Phân loại của dữ liệu nhập kho và tài liệu biên mục đi kèm phải giống nhau");
						    }
						    
						    // Check ISBN match among tracking detail and catalog item
	                        if (!Equals(wDetail.Isbn, libItemDto.Isbn))
	                        {
	                            // Add errors
	                            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
	                                key: $"warehouseTrackingDetails[{i}].libraryItem.isbn",
	                                msg: isEng
	                                    ? "ISBN is not match with warehouse tracking detail"
	                                    : "Mã ISBN không giống với dữ liệu đăng ký nhập kho");			
	                        }
	                        
	                        // Check unit price match among tracking detail and catalog item
	                        if (!Equals(wDetail.UnitPrice, libItemDto.EstimatedPrice))
	                        {
	                            // Add errors
	                            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
	                                key: $"warehouseTrackingDetails[{i}].libraryItem.estimatedPrice",
	                                msg: isEng
	                                    ? "Estimated price is not match with warehouse tracking detail"
	                                    : "Giá tiền tài liệu không giống với dữ liệu đăng ký nhập kho");	
	                        }
					    }
				    }
				    else if (wDetail.LibraryItemId == 0 && wDetail.LibraryItem != null)
				    {
					    // Declare library item
					    var libItem = wDetail.LibraryItem;
						
					    // Check transaction type
					    if (wDetail.StockTransactionType != StockTransactionType.New)
					    {
						    // Add error
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].stockTransactionType",
							    msg: isEng 
									? $"Transaction type must be '{StockTransactionType.New.ToString()}' when add tracking detail with item's catalog information" 
									: $"Loại biến động kho cần chuyển sang '{StockTransactionType.New.GetDescription()}' khi tạo dữ liệu nhập kho đi kèm với thông tin biên mục");
					    }
					    
					    // Validate library item
					    var libItemValidateRes = await ValidatorExtensions.ValidateAsync(wDetail.LibraryItem);
					    if (libItemValidateRes != null && !libItemValidateRes.IsValid)
					    {
						    // Convert ValidationResult to ValidationProblemsDetails.Errors
						    var errors = libItemValidateRes.ToProblemDetails().Errors;
						    foreach (var error in errors)
						    {
							    foreach (var value in error.Value)
							    {
								    // Add error
								    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
									    key: $"warehouseTrackingDetails[{i}].{error.Key}",
									    msg: value);
							    }
						    }
					    }
					    
					    // Select list of author ids
					    var authorIds = libItem.LibraryItemAuthors
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
					    
					    // Check same category with warehouse tracking
					    if (!Equals(wDetail.CategoryId, libItem.CategoryId))
					    {
						    // Add errors
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].libraryItem.categoryId",
							    msg: isEng
								    ? "Category among warehouse tracking detail with catalog item must be the same"
								    : "Phân loại của dữ liệu nhập kho và tài liệu biên mục đi kèm phải giống nhau");
					    }
					    
					    // Check exist cover image
					    if (!string.IsNullOrEmpty(libItem.CoverImage))
					    {
						    // Initialize field
						    var isImageOnCloud = true;

						    // Extract provider public id
						    var publicId = StringUtils.GetPublicIdFromUrl(libItem.CoverImage);
						    if (publicId != null) // Found
						    {
							    // Process check exist on cloud			
							    isImageOnCloud = (await _cloudService.IsExistAsync(publicId, FileType.Image)).Data is true;
						    }

						    if (!isImageOnCloud || publicId == null) // Not found image or public id
						    {
							    // Add error
							    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
								    key: $"warehouseTrackingDetails[{i}].libraryItem.coverImage",
								    msg: await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
						    }
					    }
					    
					    // Check exist category id
					    var categoryDto = (await _cateService.GetByIdAsync(wDetail.CategoryId)).Data as CategoryDto;
					    if (categoryDto == null)
					    {
						    // Add error
						    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].categoryId",
							    msg: StringUtils.Format(msg, isEng ? "item category" : "phân loại tài liệu"));
					    }
				    
					    // Check exist condition id 
					    var isConditionExist = (await _conditionService.AnyAsync(c => c.ConditionId == wDetail.ConditionId)).Data is true;
					    if (!isConditionExist)
					    {
						    // Add error
						    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].conditionId",
							    msg: StringUtils.Format(msg, isEng ? "item condition" : "trình trạng tài liệu"));
					    }
					    
					    /* Do not create library item instance when create warehouse tracking detail
					    // Initialize hash set of string to check unique of barcode
					    var itemInstanceBarcodes = new HashSet<string>();
					    // Iterate each library item instance (if any) to check valid data
			            var listItemInstances = libItem.LibraryItemInstances.ToList();
			            for (int j = 0; j < listItemInstances.Count; ++j)
			            {
			                var iInstance = listItemInstances[j];

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
			                            key: $"warehouseTrackingDetails[{i}].libraryItem.libraryItemInstances[{j}].barcode",
			                            msg: StringUtils.Format(errMsg, $"'{iInstance.Barcode}'"));
			                    }
			                    else
			                    {
			                        // Try to validate with category prefix
			                        var isValidBarcode =
			                            StringUtils.IsValidBarcodeWithPrefix(iInstance.Barcode, categoryDto!.Prefix);
			                        if (!isValidBarcode)
			                        {
			                            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
			                            // Add errors
			                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
			                                key: $"warehouseTrackingDetails[{i}].libraryItem.libraryItemInstances[{j}].barcode",
			                                msg: StringUtils.Format(errMsg, $"'{categoryDto.Prefix}'"));
			                        }
			                        
			                        // Try to validate barcode length
			                        var barcodeNumLength = iInstance.Barcode.Length - categoryDto.Prefix.Length; 
			                        if (barcodeNumLength != _appSettings.InstanceBarcodeNumLength) // Different from threshold value
			                        {
			                            // Add errors
			                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
			                                key: $"warehouseTrackingDetails[{i}].libraryItem.libraryItemInstances[{j}].barcode",
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
			                        key: $"warehouseTrackingDetails[{i}].libraryItem.libraryItemInstances[{j}].barcode",
			                        msg: isEng
			                            ? $"Barcode '{iInstance.Barcode}' is duplicated"
			                            : $"Số đăng ký cá biệt '{iInstance.Barcode}' đã bị trùng"
			                    );
			                }

			                // Default status
			                iInstance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
			                // Boolean 
			                iInstance.IsDeleted = false;
			            }
					    
			            // Check whether total instance is equal to total warehouse tracking detail registered item
			            if (wDetail.ItemTotal < listItemInstances.Count || wDetail.ItemTotal > listItemInstances.Count)
			            {
				            // Add errors
				            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					            key: $"warehouseTrackingDetails[{i}].itemTotal",
					            msg: isEng
						            ? "Total item instance must equal to registered warehouse tracking item total"
						            : "Tổng số lượng bản sao biên mục phải bằng với tổng số lượng tài liệu đăng ký nhập kho");	
			            }
			            
			            */
			            
					    // Check ISBN match among tracking detail and catalog item
			            if (!Equals(wDetail.Isbn, libItem.Isbn))
			            {
				            // Add errors
				            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					            key: $"warehouseTrackingDetails[{i}].libraryItem.isbn",
					            msg: isEng
						            ? "ISBN is not match with warehouse tracking detail"
						            : "Mã ISBN không giống với dữ liệu đăng ký nhập kho");			
			            }
			            else
			            {
				            // Check exist ISBN
				            var isIsbnExist = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
					            .AnyAsync(whd => Equals(whd.Isbn, wDetail.Isbn));
				            if (isIsbnExist)
				            {
					            // Add errors
					            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						            key: $"warehouseTrackingDetails[{i}].libraryItem.isbn",
						            msg: isEng
							            ? "ISBN already exist"
							            : "Mã ISBN đã tồn tại");	
				            }
			            }
			            
			            // Check unit price match among tracking detail and catalog item
			            if (!Equals(wDetail.UnitPrice, libItem.EstimatedPrice))
			            {
				            // Add errors
				            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					            key: $"warehouseTrackingDetails[{i}].libraryItem.estimatedPrice",
					            msg: isEng
						            ? "Estimated price is not match with warehouse tracking detail"
						            : "Giá tiền tài liệu không giống với dữ liệu đăng ký nhập kho");	
			            }
			            
					    // Add library item default value
					    libItem.IsTrained = false;
					    libItem.IsDeleted = false;
					    libItem.CanBorrow = false;
				    }
				    else
				    {
					    // Create without including cataloged item or item need to be cataloged along with
					    
					    // Check exist ISBN
					    var isIsbnExist = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
						    .AnyAsync(whd => Equals(whd.Isbn, wDetail.Isbn));
					    if (isIsbnExist)
					    {
						    // Add errors
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: $"warehouseTrackingDetails[{i}].isbn",
							    msg: isEng
								    ? "ISBN already exist"
								    : "Mã ISBN đã tồn tại");	
					    }
				    }
				    
	                // Assign barcode range to warehouse tracking detail
	                wDetail.BarcodeRangeFrom = barcodeRangeRes.BarcodeRangeFrom;
	                wDetail.BarcodeRangeTo = barcodeRangeRes.BarcodeRangeTo;
	                wDetail.HasGlueBarcode = false;
	                
				    // Check isbn uniqueness
				    if (string.IsNullOrEmpty(wDetail.Isbn) || !uniqueIsbnSet.Add(wDetail.Isbn))
				    {
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"warehouseTrackingDetails[{i}].isbn",
						    msg: isEng ? $"ISBN '{wDetail.Isbn}' is duplicated" : $"Mã ISBN '{wDetail.Isbn}' đã bị trùng");
				    }
				    
				    // Initialize collection of transaction type
				    var transactionTypes = new List<StockTransactionType>();
				    // Check for invalid detail tracking type
				    var transactionTypeRelations = WarehouseTrackingUtils.TransactionTypeRelations;
				    switch (dto.TrackingType)
				    {
					    case TrackingType.StockIn:
						    // Try to retrieve all detail transaction type within stock-in tracking type
						    transactionTypeRelations.TryGetValue(TrackingType.StockIn, out transactionTypes);
						    break;
					    case TrackingType.StockOut:
						    // Try to retrieve all detail transaction type within stock-in tracking type
						    transactionTypeRelations.TryGetValue(TrackingType.StockOut, out transactionTypes);
						    break;
					    case TrackingType.StockChecking:
						    // Try to retrieve all detail transaction type within stock checking tracking type
						    transactionTypeRelations.TryGetValue(TrackingType.StockChecking, out transactionTypes);
						    break;
					    case TrackingType.SupplementRequest:
						    // Try to retrieve all detail transaction type within supplement request tracking type
						    transactionTypeRelations.TryGetValue(TrackingType.SupplementRequest, out transactionTypes);
						    break;
				    }
				    
			        if (transactionTypes == null || !transactionTypes.Any())
		            {
		                // Add error
		                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                key: $"warehouseTrackingDetails[{i}].stockTransactionType",
			                msg: isEng 
				                ? $"Not found any appropriate stock transaction type for tracking type '{dto.TrackingType.ToString()}'" 
				                : "Không tìm thấy loại biến động kho phù hợp");
		            }
		            else
		            {
		                // Convert to str collection
		                var transactionList = transactionTypes.Select(x => x.ToString()).ToList();
		                // Convert record transaction type to str
		                var recordTransactionTypeStr = wDetail.StockTransactionType.ToString();
		                
		                if (!transactionList.Contains(recordTransactionTypeStr))
		                {
			                // Add error
			                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				                key: $"warehouseTrackingDetails[{i}].stockTransactionType",
				                msg: isEng
					                ? $"'${recordTransactionTypeStr} is invalid. Transaction types must include in '{String.Join(",", transactionList)}'"
					                : $"Loại biến động kho yêu cầu '{String.Join(",", transactionList)}'");
		                }
		            }
		            
		            // Set tracking detail lib item id to null to ensure no SQL conflict invoke
		            if(wDetail.LibraryItemId == 0) wDetail.LibraryItemId = null; 
                }
                else
                {
	                // Mark as fail to create
	                // var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
	                // var customMsg = isEng
		               //  ? "Cannot generate barcode range for one or many warehouse tracking detail"
		               //  : "Lỗi xảy ra khi tạo số ĐKCB cho 1 hoặc nhiều tài liệu đăng ký nhập kho";
	                // return new ServiceResult(ResultCodeConst.SYS_Fail0001, $"{msg}. {customMsg}");
	                return generateRes;
                }
                
                // Add accumulate skip
                accumulateDic[wDetail.CategoryId] += wDetail.ItemTotal;
		    }

		    // Check if any error invoke
		    if (customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);

		    // Generate receipt number
		    var receiptNum = $"PN{StringUtils.GenerateRandomCodeDigits(8)}";
		    // Add necessary fields
		    dto.ReceiptNumber = receiptNum;
		    dto.TotalItem = dto.WarehouseTrackingDetails.Any() ? dto.WarehouseTrackingDetails.Count : 0;
		    dto.TotalAmount = dto.WarehouseTrackingDetails.Any() ? dto.WarehouseTrackingDetails.Sum(wtd => wtd.TotalAmount) : 0;
		    dto.TrackingType = TrackingType.StockIn;
		    dto.Status = WarehouseTrackingStatus.Draft;
		    
		    // Process add new warehouse tracking with details
		    await _unitOfWork.Repository<WarehouseTracking, int>().AddAsync(_mapper.Map<WarehouseTracking>(dto));
		    // Save DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
			    if(dto.WarehouseTrackingDetails.Any())
			    {
				    var customMsg = isEng
					    ? $"total {dto.WarehouseTrackingDetails.Count} warehouse tracking details have been saved"
					    : $"tổng {dto.WarehouseTrackingDetails.Count} tài liệu đăng ký nhập kho đã được thêm";
				    return new ServiceResult(ResultCodeConst.SYS_Success0001, $"{msg}, {customMsg}");
			    }
	            
			    return new ServiceResult(ResultCodeConst.SYS_Success0001, msg);
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
		    throw new Exception("Error invoke when process create warehouse tracking with details");
	    }
    }

    
	public async Task<IServiceResult> CreateAndImportDetailsAsync(
        WarehouseTrackingDto dto,
        IFormFile? trackingDetailsFile,
        List<IFormFile> coverImageFiles,
        string[]? scanningFields,
        DuplicateHandle? duplicateHandle)
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
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Custom errors
            var customErrors = new Dictionary<string, string[]>();
            
            // Check exist supplier
            var isSupplierExist = (await _supplierService.AnyAsync(s => s.SupplierId == dto.SupplierId)).Data is true;
            if (!isSupplierExist)
            {
	            // Not found {0}
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
		            StringUtils.Format(errMsg, isEng 
					? "supplier" 
					: "nhà phân phối hoặc nhà cung cấp"));
            }
            
            // Check whether any error invoke
            if(customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);
            
            // Generate receipt number
            var receiptNum = $"PN{StringUtils.GenerateRandomCodeDigits(8)}";
            // Add necessary fields
            dto.ReceiptNumber = receiptNum;
            dto.Status = WarehouseTrackingStatus.Draft;
            
            // Additional message
            var totalInvalidWhFromDuplicateHandle = 0;
            var totalInvalidItemFromDuplicateHandle = 0;
            // Initialize list of unknown item to process export Excel file
            var unknownItems = new List<LibraryItemDto>();
            // Try to add tracking details (if any)
            if (trackingDetailsFile != null)
            {
                // Check exist file
                if (trackingDetailsFile.Length == 0)
                {
                    return new ServiceResult(ResultCodeConst.File_Warning0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
                }

                // Validate import file 
                validationResult = await ValidatorExtensions.ValidateAsync(trackingDetailsFile);
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
                
                // Process read csv file (warehouse tracking detail)
                var readWhResp =
                    CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<WarehouseTrackingDetailCsvRecord>(
                        file: trackingDetailsFile, 
                        config: csvConfig,
                        props: new ExcelProps()
                        {
	                        // Header start from row 2-3
	                        FromRow = 2,
	                        ToRow = 3,
	                        // Start from col
	                        FromCol = 1,
	                        // Start read data index
	                        StartRowIndex = 4,
	                        // Worksheet index
	                        WorkSheetIndex = 0
                        },
                        encodingType: null,
                        systemLang: lang);
                if(readWhResp.Errors.Any())
                {
                    return await ReturnWrongImportDataAsync(worksheetIndex: "1", errors: readWhResp.Errors);
                }
                
                // Exclude all data without item name
                readWhResp.Records = readWhResp.Records.Where(r => !string.IsNullOrEmpty(r.ItemName)).ToList();
                
                // Try to detect wrong data (warehouse tracking detail)
                var wrongDataErrs = await DetectWrongDataAsync(
	                trackingType: dto.TrackingType,
	                records: readWhResp.Records,
	                lang: lang);
                if (wrongDataErrs.Any())
                {
	                foreach (var err in wrongDataErrs)
	                {
		                // Check exist err in dictionary
		                if (readWhResp.Errors.ContainsKey(err.Key)) // already exist
		                {
			                readWhResp.Errors[err.Key] = readWhResp.Errors[err.Key]
				                .Concat(err.Value.ToArray()).ToArray();
		                }
		                else // not exist
		                {
			                readWhResp.Errors.Add(err.Key, err.Value.ToArray());
		                }
	                }

	                return await ReturnWrongImportDataAsync(worksheetIndex: "1", errors: readWhResp.Errors);
                }
                
                // Detect duplicates (warehouse tracking detail)
                var whDuplicateResult = DetectDuplicatesInFile(readWhResp.Records, scanningFields ?? [], lang);
                if (whDuplicateResult.Errors.Count != 0 && duplicateHandle == null) // Has not selected any handle options yet
                {
	                var parsedDicErrors = whDuplicateResult.Errors.Select(
		                dic => new KeyValuePair<int, string[]>(
			                dic.Key, dic.Value.ToArray())).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	                
	                return await ReturnWrongImportDataAsync(worksheetIndex: "1", errors: parsedDicErrors);
                }
                if (whDuplicateResult.Errors.Count != 0 && duplicateHandle != null) // Selected any handle options
                {
	                // Handle duplicates
	                var handleResult = CsvUtils.HandleDuplicates(
		                readWhResp.Records, whDuplicateResult.Duplicates, (DuplicateHandle) duplicateHandle, lang);
	                // Update records
	                readWhResp.Records = handleResult.handledRecords;
	                // Update invalid item
	                totalInvalidWhFromDuplicateHandle = handleResult.totalInvalidItem;
                }
                
                // Process read csv file (library item)
                var readLibResp =
	                CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<LibraryItemCsvRecordDto>(
		                file: trackingDetailsFile, 
		                config: csvConfig,
		                props: new ExcelProps()
		                {
			                // Header start from row 2-3
			                FromRow = 2,
			                ToRow = 2,
			                // Start from col
			                FromCol = 1,
			                // Start read data index
			                StartRowIndex = 3,
			                // Worksheet 1
			                WorkSheetIndex = 1
		                },
		                encodingType: null,
		                systemLang: lang);
                if(readLibResp.Errors.Any())
                {
	                return await ReturnWrongImportDataAsync(worksheetIndex: "2", errors: readLibResp.Errors);
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
                
                // Try to detect wrong data (library item)
                wrongDataErrs = (await _itemService.DetectWrongImportDataAsync(
	                startRowIndex: 3,
					records: readLibResp.Records,
	                coverImageNames: imageFileNames)).Data as Dictionary<int, List<string>>;
                if (wrongDataErrs != null && wrongDataErrs.Any())
                {
	                foreach (var err in wrongDataErrs)
	                {
		                // Check exist err in dictionary
		                if (readLibResp.Errors.ContainsKey(err.Key)) // already exist
		                {
			                readLibResp.Errors[err.Key] = readLibResp.Errors[err.Key]
				                .Concat(err.Value.ToArray()).ToArray();
		                }
		                else // not exist
		                {
			                readLibResp.Errors.Add(err.Key, err.Value.ToArray());
		                }
	                }

	                return await ReturnWrongImportDataAsync(worksheetIndex: "2", errors: readLibResp.Errors);
                }
                
                // Detect duplicates (library item)
                if ((await _itemService.DetectDuplicatesInFileAsync(
	                    readLibResp.Records, scanningFields ?? []))
                    .Data is (Dictionary<int, List<string>> errors, Dictionary<int, List<int>> duplicates))
                {
	                if (errors.Count != 0 && duplicateHandle == null) // Has not selected any handle options yet
	                {
		                var parsedDicErrors = errors.Select(
			                dic => new KeyValuePair<int, string[]>(
				                dic.Key, dic.Value.ToArray())).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		                return await ReturnWrongImportDataAsync(worksheetIndex: "2", errors: parsedDicErrors);
	                }
	                if (errors.Count != 0 && duplicateHandle != null) // Selected any handle options
	                {
		                // Handle duplicates
		                var handleResult = CsvUtils.HandleDuplicates(
			                readLibResp.Records, duplicates, (DuplicateHandle) duplicateHandle, lang);
		                // Update records
		                readLibResp.Records = handleResult.handledRecords;
		                // Update total invalid item
		                totalInvalidItemFromDuplicateHandle = handleResult.totalInvalidItem;
	                }
                }
                
                // Handle upload images (Image name | URL) after check all duplicate or wrong data
                var imageUrlDic = new Dictionary<string, string>();
                foreach (var coverImage in coverImageFiles)
                {
	                // Try to validate file
	                var validateResult = await 
		                new ImageTypeValidator(lang.ToString() ?? SystemLanguage.English.ToString()).ValidateAsync(coverImage);
	                if (!validateResult.IsValid)
	                {
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
		                // Fail to upload imag
		                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0001);
		                return new ServiceResult(ResultCodeConst.Cloud_Fail0001,
			                errMsg + (isEng ? $"With image name '{coverImage}'" : $"Với tên hình '{coverImage}'"));
	                }
	                else
	                {
		                // Add to dic
		                imageUrlDic.Add(coverImage.FileName, uploadResult.SecureUrl);
	                }
                }
                
				// Retrieve all existing categories
				var categories = (await _cateService.GetAllAsync()).Data as List<CategoryDto>;
				if (categories == null || !categories.Any())
				{
					var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(msg, isEng
							? "categories to process import"
							: "phân loại để tiến hành import"));
				}
            
				// Retrieve all existing conditions
				var conditions = (await _conditionService.GetAllAsync()).Data as List<LibraryItemConditionDto>;
				if (conditions == null || !conditions.Any())
				{
					var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(msg, isEng
							? "conditions to process import"
							: "danh sách tình trạng sách để tiến hành import"));
				}
				
				// Retrieve all authors to add to lib item (if any)
				var authors = (await _authorService.GetAllAsync()).Data as List<AuthorDto>;
				
				// Convert warehouse tracking csv record to dto
				var libraryItemDtos = readLibResp.Records
					.Select(r => 
						r.ToLibraryItemDto(
							imageUrlDic: imageUrlDic, 
							categories: categories, 
							authors: authors)).ToList();
				// Convert library item csv record to dto 
                var warehouseTrackingDetailDtos = readWhResp.Records
	                .Select(r => r.ToWarehouseTrackingDetailDto(
		                conditions: conditions,
		                categories: categories))
	                .ToList();
				
				// Process compare warehouse tracking details with library items
				var compareRes = await CompareTrackingDetailToItemAsync(
					trackingStartRowIndex: 4,
					libItemStartRowIndex: 3,
					whTrackingDetails: warehouseTrackingDetailDtos,
					libItems: libraryItemDtos);
				if (compareRes.Errors.Any() || compareRes.UnknownItems.Any())
				{
					
					return new ServiceResult(ResultCodeConst.SYS_Fail0008,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), compareRes.Errors);
				}
				
				// Add range unknown items (if any)
				if(compareRes.UnknownItems.Any()) unknownItems.AddRange(compareRes.UnknownItems);
				
				// Initialize skip item dic
				var skipItemDic = new Dictionary<int, int>(); // KeyPair (categoryId, totalSkipVal)
				// Initialize dictionary to accumulating skip item
				var accumulateDic = new Dictionary<int, int>();
				// Progress import warehouse tracking detail
				foreach (var whDetailDto in warehouseTrackingDetailDtos)
				{
					// Retrieve skip item from dic with specific category id
					if (!skipItemDic.ContainsKey(whDetailDto.CategoryId))
					{
						// Add default skip val
						skipItemDic[whDetailDto.CategoryId] = 0;
						// Add default accumulate val
						accumulateDic[whDetailDto.CategoryId] = 0;
					}
					else
					{
						// Increase skip val based on warehouse tracking item total
						skipItemDic[whDetailDto.CategoryId] = accumulateDic[whDetailDto.CategoryId];
					}
					
					var skipVal = skipItemDic[whDetailDto.CategoryId];
					// Generate barcode range based on registered warehouse tracking detail
					var barcodeRangeRes = (await _itemInstanceService.GenerateBarcodeRangeAsync(
						categoryId: whDetailDto.CategoryId,
						totalItem: whDetailDto.ItemTotal,
						skipItem: skipVal)).Data as GenerateBarcodeRangeResultDto;

					if (barcodeRangeRes != null && barcodeRangeRes.Barcodes.Any())
					{
						// Determine stock transaction type to process add library item along with
						switch (whDetailDto.StockTransactionType)
						{
							case StockTransactionType.New:
								// Check whether add warehouse tracking detail with catalog information
								// Retrieve library item to be cataloged by isbn
								var libToCatalogDto = libraryItemDtos.FirstOrDefault(li => Equals(li.Isbn, whDetailDto.Isbn));
								if (libToCatalogDto != null)
								{
									// Add to warehouse tracking detail
									whDetailDto.LibraryItem = libToCatalogDto;
									
									/* Do not create instance when create warehouse tracking detail
									// Initialize list of library item instance
                                    var itemInstances = new List<LibraryItemInstanceDto>();
                                    // Iterate each barcode generated to add library item instance and its default condition history
                                    foreach (var barcode in barcodeRangeRes.Barcodes)
                                    {
                                    	itemInstances.Add(new ()
                                    	{
                                    		Barcode = barcode,
                                    		Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
                                    		LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
                                    		{
                                    			new()
                                    			{
                                    				ConditionId = whDetailDto.ConditionId,
                                    			}
                                    		}
                                    	});
                                    }
									
									// Update library item inventory
									if (whDetailDto.LibraryItem.LibraryItemInventory != null)
									{
										whDetailDto.LibraryItem.LibraryItemInventory.TotalUnits = itemInstances.Count;
									}
									
									// Assign instances to item
									whDetailDto.LibraryItem.LibraryItemInstances = itemInstances;
									*/
								}
								break;
							case StockTransactionType.Additional:
								/* Do not create instance when create warehouse tracking detail
								// Process add range instance to library item
								var addRes = await _itemInstanceService.AddRangeBarcodeWithoutSaveChangesAsync(
									isbn: whDetailDto.Isbn!,
									conditionId: whDetailDto.ConditionId,
									barcodeRangeFrom: barcodeRangeRes.BarcodeRangeFrom,
									barcodeRangeTo: barcodeRangeRes.BarcodeRangeTo);
								if (addRes.Data is int libItemId && libItemId > 0)
								{
									// Assign library item id to warehouse tracking detail
									whDetailDto.LibraryItemId = libItemId;
								}
								else
								{
									// Mark as fail to create
									var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
									var customMsg = isEng
										? $"Cannot add range library item instances for warehouse tracking detail '{whDetailDto.ItemName}'"
										: $"Không thể thêm các bản sao cho dữ liệu nhập kho '{whDetailDto.ItemName}'";
									return new ServiceResult(ResultCodeConst.SYS_Fail0001, $"{msg}.{customMsg}");
								}
								*/
								
								// Assign library item id 
								var existingLibItem = (await _itemService.GetWithSpecAsync(new BaseSpecification<LibraryItem>(li => 
									Equals(li.Isbn, whDetailDto.Isbn)))).Data as LibraryItemDto;
								if (existingLibItem != null) whDetailDto.LibraryItemId = existingLibItem.LibraryItemId;
								break;
							case StockTransactionType.Damaged or StockTransactionType.Outdated or StockTransactionType.Lost:
								break;
							case StockTransactionType.Reorder:
								break;
							case StockTransactionType.Other:
								break;
						}
						
						// Update warehouse tracking barcode range from-to
						whDetailDto.BarcodeRangeFrom = barcodeRangeRes.BarcodeRangeFrom;
						whDetailDto.BarcodeRangeTo = barcodeRangeRes.BarcodeRangeTo;
						// Mark default is not glue barcode yet
						whDetailDto.HasGlueBarcode = false;
					}
					else
					{
						// Mark as fail to create
						var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
						var customMsg = isEng
							? "Cannot generate barcode range for one or many warehouse tracking detail"
							: "Lỗi xảy ra khi tạo số ĐKCB cho 1 hoặc nhiều tài liệu đăng ký nhập kho";
						return new ServiceResult(ResultCodeConst.SYS_Fail0001, $"{msg}.{customMsg}");
					}
					
					// Add to warehouse tracking
					dto.WarehouseTrackingDetails.Add(whDetailDto);
					
					// Add warehouse tracking inventory
					// Total item <- Number of warehouse tracking details  
					var totalItem = dto.WarehouseTrackingDetails.Count;
					// Total instance item <- Sum of each warehouse tracking detail's item total
					var totalInstanceItem = dto.WarehouseTrackingDetails.Count != 0
						? dto.WarehouseTrackingDetails.Select(wtd => wtd.ItemTotal).Sum() 
						: 0;
					// Total cataloged item <- Any tracking detail request along with libraryItemId > 0 or libraryItem != null
					var totalCatalogedItem = dto.WarehouseTrackingDetails
						.Count(wtd => (wtd.LibraryItemId != null && wtd.LibraryItemId > 0) || wtd.LibraryItem != null);
					
					// Initialize warehouse tracking inventory
					dto.WarehouseTrackingInventory = new()
					{
						TotalItem = totalItem,
						TotalInstanceItem = totalInstanceItem,
						TotalCatalogedItem = totalCatalogedItem,
						TotalCatalogedInstanceItem = 0
					};
					
					// Add accumulate skip
					accumulateDic[whDetailDto.CategoryId] += whDetailDto.ItemTotal;
				}     
            }

            // Progress add new warehouse tracking 
            await _unitOfWork.Repository<WarehouseTracking, int>().AddAsync(_mapper.Map<WarehouseTracking>(dto));
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
	            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
	            if(dto.WarehouseTrackingDetails.Any())
	            {
		            // Count total invalid item
		            var totalInvalidItem = 
			            totalInvalidItemFromDuplicateHandle + totalInvalidWhFromDuplicateHandle;
		            // Initialize duplicate message
		            var duplicateMsg =
			            duplicateHandle == DuplicateHandle.Replace
				            ? isEng ? "replaced" : "thay thế"
				            : duplicateHandle == DuplicateHandle.Skip
					            ? isEng ? "skipped" : "bỏ qua"
					            : string.Empty;
		            
					var customMsg = totalInvalidItem == 0 && string.IsNullOrEmpty(duplicateMsg)
                        ? isEng 
	                        ? $"total {dto.WarehouseTrackingDetails.Count} warehouse tracking details have been saved"
	                        : $"tổng {dto.WarehouseTrackingDetails.Count} thông tin tài liệu đã được thêm"
                        : isEng
							? $"total {dto.WarehouseTrackingDetails.Count} warehouse tracking details have been saved, " +
							  $"total '{totalInvalidItem} being {duplicateMsg}'"
							: $"tổng {dto.WarehouseTrackingDetails.Count} thông tin tài liệu đã được thêm, " +
							  $"tổng '{totalInvalidItem} đã bị {duplicateMsg}'";
                    return new ServiceResult(ResultCodeConst.SYS_Success0001, $"{msg}, {customMsg}");
				}
	            
	            return new ServiceResult(ResultCodeConst.SYS_Success0001, msg);
            }
            
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
            throw new Exception("Error invoke while process create and import warehouse tracking details");
        }
    }
	
    private async Task<Dictionary<int, List<string>>> DetectWrongDataAsync(
	    TrackingType trackingType,
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

		    // Initialize bool to mark whether check stock transaction type
		    var transactionTypeCheck = false;
		    // Validate transaction type
		    if (!Enum.TryParse(record.StockTransactionType, true, out StockTransactionType stockTransactionType))
		    {
			    // Try to check stock transaction type desc
			    if (Equals(record.StockTransactionType, StockTransactionType.New.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.New;
			    }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Additional.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.Additional;
			    }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Damaged.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.Damaged;
			    }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Lost.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.Lost;
			    }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Outdated.GetDescription()))
                {
				    stockTransactionType = StockTransactionType.Outdated;
                }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Reorder.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.Reorder;
			    }
			    else if (Equals(record.StockTransactionType, StockTransactionType.Other.GetDescription()))
			    {
				    stockTransactionType = StockTransactionType.Other;
			    }
			    else
			    {
				    // Add error
				    rowErrors.Add(isEng 
					    ? $"Stock transaction type '{record.StockTransactionType}' is invalid" 
					    : $"Loại biến động kho '{record.StockTransactionType}' không hợp lệ");
			    }
			    
			    // Mark as check trans type 
			    transactionTypeCheck = true;
		    }
		    
		    // Initialize collection of transaction type
		    var transactionTypes = new List<StockTransactionType>();
		    // Check for invalid detail tracking type
		    var transactionTypeRelations = WarehouseTrackingUtils.TransactionTypeRelations;
		    switch (trackingType)
		    {
			    case TrackingType.StockIn:
				    // Try to retrieve all detail transaction type within stock-in tracking type
				    transactionTypeRelations.TryGetValue(TrackingType.StockIn, out transactionTypes);
				    break;
			    case TrackingType.StockOut:
				    // Try to retrieve all detail transaction type within stock-in tracking type
				    transactionTypeRelations.TryGetValue(TrackingType.StockOut, out transactionTypes);
				    break;
			    case TrackingType.StockChecking:
				    // Try to retrieve all detail transaction type within stock checking tracking type
				    transactionTypeRelations.TryGetValue(TrackingType.StockChecking, out transactionTypes);
				    break;
			    case TrackingType.SupplementRequest:
				    // Try to retrieve all detail transaction type within supplement request tracking type
				    transactionTypeRelations.TryGetValue(TrackingType.SupplementRequest, out transactionTypes);
				    break;
		    }
		    
	        if (transactionTypes == null || !transactionTypes.Any())
            {
                // Add error
                rowErrors.Add(isEng 
					? $"Not found any appropriate stock transaction type for tracking type '{trackingType.ToString()}'" 
					: "Không tìm thấy loại biến động kho phù hợp");
            }
            else if(!transactionTypeCheck)
            {
                // Convert to str collection
                var transactionList = transactionTypes.Select(x => x.ToString()).ToList();
                // Convert record transaction type to str
                var recordTransactionTypeStr = stockTransactionType.ToString();
                
                if (!transactionList.Contains(recordTransactionTypeStr))
                {
            	    // Add error
            	    rowErrors.Add(isEng
            		    ? $"'${recordTransactionTypeStr} is invalid. Transaction types must include in '{String.Join(", ", transactionList)}'"
            		    : $"Loại biến động kho yêu cầu '{String.Join(", ", transactionTypes.Select(t => t.GetDescription()))}'");
                }
            }
		    
		    // Check exist category
		    if ((await _cateService.AnyAsync(c =>
			        Equals(c.EnglishName.ToLower(), record.Category.ToLower()) || 
			        Equals(c.VietnameseName.ToLower(), record.Category.ToLower())
		        )).Data is false)
		    {
			    rowErrors.Add(isEng ? "Category name not exist" : "Tên phân loại không tồn tại");
		    }
		    
		    // Check exist condition
		    if ((await _conditionService.AnyAsync(c =>
			        Equals(c.EnglishName.ToLower(), record.Condition.ToLower()) ||
			        Equals(c.VietnameseName.ToLower(), record.Condition.ToLower())
		        )).Data is false)
		    {
			    rowErrors.Add(isEng ? "Condition name not exist" : "Tên tình trạng tài liệu không tồn tại");
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
			    var isIsbnExist = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(w =>
				    Equals(cleanedIsbn, w.Isbn));
			    // Required exist ISBN when tracking type is stock-out or transfer
			    if(!isIsbnExist && (trackingType == TrackingType.StockOut || trackingType == TrackingType.StockChecking)) 
			    {
				    rowErrors.Add(isEng 
					    ? $"ISBN '{record.Isbn}' must exist when tracking type is stock out or transfer" 
					    : $"Mã ISBN '{record.Isbn}' không tồn tại. Yêu cầu mã ISBN của tài liệu đã được biên mục khi xuất kho hoặc kiểm kê");
			    }
			    // Already exist ISBN
			    if (isIsbnExist)
			    {
				    // Only process check exist ISBN when stock transaction type is new
				    if (stockTransactionType == StockTransactionType.New)
				    {
					    rowErrors.Add(isEng
						    ? $"ISBN '{record.Isbn}' already existed"
						    : $"ISBN '{record.Isbn}' đã tồn tại");
				    }
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

	private async Task<IServiceResult> ReturnWrongImportDataAsync(string worksheetIndex, Dictionary<int, string[]> errors)
	{
		var errorResps = errors.Select(x => new ImportErrorResultDto()
		{	
			WorkSheetIndex = !string.IsNullOrEmpty(worksheetIndex) ? int.Parse(worksheetIndex) : 0,
			RowNumber = x.Key,
			Errors = x.Value.ToList()
		});
	        
		return new ServiceResult(ResultCodeConst.SYS_Fail0008,
			await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), new
			{
				Worksheet = worksheetIndex,
				Result = errorResps
			});
	}

	private async Task<(List<ImportErrorResultDto> Errors, List<LibraryItemDto> UnknownItems)>
		CompareTrackingDetailToItemAsync(
			int trackingStartRowIndex,
			int libItemStartRowIndex,
			List<WarehouseTrackingDetailDto> whTrackingDetails,
			List<LibraryItemDto> libItems)
	{
		// Determine current system language
		var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			LanguageContext.CurrentLanguage);
		var isEng = lang == SystemLanguage.English;

		// Initialize import error result 
		var importErrRes = new List<ImportErrorResultDto>();
		// Initialize unknown library item (ISBN not match with warehouse tracking detail)
		List<LibraryItemDto> unknownItems = new();

		// Iterate each library item (worksheet 2) (NEW transaction type handling)
		for (int i = 0; i < libItems.Count; i++)
		{
			var libItem = libItems[i];

			// Initialize list of errors
			var errors = new List<string>();

			// Try to extract library item record (if any)
			var wDetail = whTrackingDetails.FirstOrDefault(li => Equals(li.Isbn, libItem.Isbn));

			// Only process compare when exist lib item
			if (wDetail != null && wDetail.StockTransactionType == StockTransactionType.New)
			{
				// Compare category 
				if (!Equals(libItem.CategoryId, wDetail.CategoryId))
				{
					// Add error
					errors.Add(isEng
						? "Category among warehouse tracking detail with catalog item must be the same"
						: "Phân loại của dữ liệu nhập kho và tài liệu biên mục đi kèm phải giống nhau");
				}

				// Compare price
				if (!Equals(libItem.EstimatedPrice, wDetail.UnitPrice))
				{
					// Add error
					errors.Add(isEng
						? "Estimated price is not match with warehouse tracking detail"
						: "Giá tiền tài liệu không giống với dữ liệu đăng ký nhập kho");
				}
			}
			else if (wDetail != null && wDetail.StockTransactionType == StockTransactionType.Additional)
			{
				// Add error 
				errors.Add(isEng
					? $"Stock transaction type of this catalog item is valid. Please change to '{StockTransactionType.New.ToString()}'"
					: $"Loại biến động của dữ liệu nhập kho có thông tin biên mục đi kèm không hợp lệ. Vui lòng chuyển sang '{StockTransactionType.New.GetDescription()}'");
			}
			else if (wDetail == null)
			{
				// Add to unknown item list 
				unknownItems.Add(libItem);
			}

			if (errors.Any()) // Add error if found any
			{
				importErrRes.Add(new()
				{
					WorkSheetIndex = 2,
					RowNumber = libItemStartRowIndex,
					Errors = errors
				});
			}

			// Increase row index
			libItemStartRowIndex++;
		}

		// Iterate each warehouse tracking detail (worksheet 1) to compare category  (ADDITIONAL transaction type handling)
		for (int i = 0; i < whTrackingDetails.Count; i++)
		{
			var wDetail = whTrackingDetails[i];

			// Initialize list of errors
			var errors = new List<string>();

			if (wDetail.StockTransactionType == StockTransactionType.Additional)
			{
				// Check exist library item by ISBN
				var itemSpec = new BaseSpecification<LibraryItem>(li => Equals(li.Isbn, wDetail.Isbn));
				var existingItem = (await _itemService.GetWithSpecAsync(itemSpec)).Data as LibraryItemDto;
				if (existingItem == null)
				{
					// Add error 
					errors.Add(isEng
						? $"Not found any cataloged item match ISBN '{wDetail.Isbn}'. " +
						  $"Please reconsider or change stock transaction type to '{StockTransactionType.New.ToString()}'"
						: $"Không tìm thấy mã ISBN '{wDetail.Isbn}' trong tài liệu đã được biên mục. " +
						  $"Vui lòng kiểm tra lại hoặc đổi phân loại dữ liệu nhập kho sang '{StockTransactionType.New.GetDescription()}'");
				}
				else
				{
					// Compare category 
					if (!Equals(wDetail.CategoryId, existingItem.CategoryId))
					{
						// Add error
						errors.Add(isEng
							? "Category among warehouse tracking detail with cataloged item must be the same"
							: "Phân loại của dữ liệu nhập kho và tài liệu biên mục đã biên mục phải giống nhau");
					}

					// Compare price
					if (!Equals(wDetail.UnitPrice, existingItem.EstimatedPrice))
					{
						// Add error
						errors.Add(isEng
							? "Unit price of warehouse tracking detail is not match with cataloged item"
							: "Giá tiền của tài liệu dữ liệu đăng ký nhập kho không giống với tài liệu đã biên mục");
					}
	            
				}
			}
			
			if (errors.Any()) // Add error if found any
			{
				importErrRes.Add(new()
				{
					WorkSheetIndex = 1,
					RowNumber = trackingStartRowIndex,
					Errors = errors
				});
			}

			// Increase row index
			trackingStartRowIndex++;
		}

		return (importErrRes, unknownItems);
	}
 }