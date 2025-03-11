using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class WarehouseTrackingDetailService :
    GenericService<WarehouseTrackingDetail, WarehouseTrackingDetailDto, int>,
    IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>
{
    private readonly AppSettings _appSettings;
    private readonly ICloudinaryService _cloudSvc;
    
    private readonly IAuthorService<AuthorDto> _authorSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemService<LibraryItemDto> _itemSvc;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionSvc;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceSvc;
    private readonly IWarehouseTrackingService<WarehouseTrackingDto> _trackingSvc;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventorySvc;

    public WarehouseTrackingDetailService(
	    IAuthorService<AuthorDto> authorSvc,
	    ICategoryService<CategoryDto> cateSvc,
	    ILibraryItemService<LibraryItemDto> itemSvc,
	    ILibraryItemInventoryService<LibraryItemInventoryDto> inventorySvc,
	    ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceSvc,
	    ILibraryItemConditionService<LibraryItemConditionDto> conditionSvc,
	    IWarehouseTrackingService<WarehouseTrackingDto> trackingSvc,
	    ICloudinaryService cloudSvc,
	    IOptionsMonitor<AppSettings> monitor,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _appSettings = monitor.CurrentValue;
	    _authorSvc = authorSvc;
	    _cateSvc = cateSvc;
	    _inventorySvc = inventorySvc;
	    _cloudSvc = cloudSvc;
	    _itemSvc = itemSvc;
	    _itemInstanceSvc = itemInstanceSvc;
        _trackingSvc = trackingSvc;
        _conditionSvc = conditionSvc;
    }

    public async Task<IServiceResult> GetDetailAsync(int id)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
			
			// Build specification
			var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x => x.TrackingDetailId == id);
			// Retrieve entity with spec
			var existingEntity =
				await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAndSelectorAsync(baseSpec,
					selector: w => new WarehouseTrackingDetail()
					{
						TrackingDetailId = w.TrackingDetailId,
						ItemName = w.ItemName,
						ItemTotal = w.ItemTotal,
						Isbn = w.Isbn,
						UnitPrice = w.UnitPrice,
						TotalAmount = w.TotalAmount,
						StockTransactionType = w.StockTransactionType,
						TrackingId = w.TrackingId,
						LibraryItemId = w.LibraryItemId,
						CategoryId = w.CategoryId,
						ConditionId = w.ConditionId,
						BarcodeRangeFrom = w.BarcodeRangeFrom,
						BarcodeRangeTo = w.BarcodeRangeFrom,
						HasGlueBarcode = w.HasGlueBarcode,
						CreatedAt = w.CreatedAt,
						UpdatedAt = w.UpdatedAt,
						CreatedBy = w.CreatedBy,
						UpdatedBy = w.UpdatedBy,
						LibraryItem = w.LibraryItem != null 
							? new LibraryItem()
							{
								LibraryItemId = w.LibraryItem.LibraryItemId,
			                    Title = w.LibraryItem.Title,
			                    SubTitle = w.LibraryItem.SubTitle,
			                    Responsibility = w.LibraryItem.Responsibility,
			                    Edition = w.LibraryItem.Edition,
			                    EditionNumber = w.LibraryItem.EditionNumber,
			                    Language = w.LibraryItem.Language,
			                    OriginLanguage = w.LibraryItem.OriginLanguage,
			                    Summary = w.LibraryItem.Summary,
			                    CoverImage = w.LibraryItem.CoverImage,
			                    PublicationYear = w.LibraryItem.PublicationYear,
			                    Publisher = w.LibraryItem.Publisher,
			                    PublicationPlace = w.LibraryItem.PublicationPlace,
			                    ClassificationNumber = w.LibraryItem.ClassificationNumber,
			                    CutterNumber = w.LibraryItem.CutterNumber,
			                    Isbn = w.LibraryItem.Isbn,
			                    Ean = w.LibraryItem.Ean,
			                    EstimatedPrice = w.LibraryItem.EstimatedPrice,
			                    PageCount = w.LibraryItem.PageCount,
			                    PhysicalDetails = w.LibraryItem.PhysicalDetails,
			                    Dimensions = w.LibraryItem.Dimensions,
			                    AccompanyingMaterial = w.LibraryItem.AccompanyingMaterial,
			                    Genres = w.LibraryItem.Genres,
			                    GeneralNote = w.LibraryItem.GeneralNote,
			                    BibliographicalNote = w.LibraryItem.BibliographicalNote,
			                    TopicalTerms = w.LibraryItem.TopicalTerms,
			                    AdditionalAuthors = w.LibraryItem.AdditionalAuthors,
			                    CategoryId = w.LibraryItem.CategoryId,
			                    ShelfId = w.LibraryItem.ShelfId,
			                    GroupId = w.LibraryItem.GroupId,
			                    Status = w.LibraryItem.Status,
			                    IsDeleted = w.LibraryItem.IsDeleted,
			                    IsTrained = w.LibraryItem.IsTrained,
			                    CanBorrow = w.LibraryItem.CanBorrow,
			                    TrainedAt = w.LibraryItem.TrainedAt,
			                    CreatedAt = w.LibraryItem.CreatedAt,
			                    UpdatedAt = w.LibraryItem.UpdatedAt,
			                    UpdatedBy = w.LibraryItem.UpdatedBy,
			                    CreatedBy = w.LibraryItem.CreatedBy,
			                    // References
			                    Category = w.LibraryItem.Category,
			                    Shelf = w.LibraryItem.Shelf,
			                    LibraryItemInventory = w.LibraryItem.LibraryItemInventory,
			                    LibraryItemInstances = w.LibraryItem.LibraryItemInstances.Select(li => new LibraryItemInstance()
			                    {
				                    LibraryItemInstanceId = li.LibraryItemInstanceId,
				                    LibraryItemId = li.LibraryItemId,
				                    Barcode = li.Barcode,
				                    Status = li.Status,
				                    CreatedAt = li.CreatedAt,
				                    UpdatedAt = li.UpdatedAt,
				                    UpdatedBy = li.UpdatedBy,
				                    CreatedBy = li.CreatedBy,
				                    IsDeleted = li.IsDeleted,
				                    LibraryItemConditionHistories = li.LibraryItemConditionHistories
			                    }).ToList(),
			                    LibraryItemReviews = w.LibraryItem.LibraryItemReviews,
			                    LibraryItemAuthors = w.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
			                    {
			                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
			                        LibraryItemId = ba.LibraryItemId,
			                        AuthorId = ba.AuthorId,
			                        Author = ba.Author
			                    }).ToList()
							}
							: null,
						WarehouseTracking = new WarehouseTracking()
						{
							TrackingId = w.WarehouseTracking.TrackingId,
							SupplierId = w.WarehouseTracking.SupplierId,
							ReceiptNumber = w.WarehouseTracking.ReceiptNumber,
							TotalItem = w.WarehouseTracking.TotalItem,
							TotalAmount = w.WarehouseTracking.TotalAmount,
							TrackingType = w.WarehouseTracking.TrackingType,
							TransferLocation = w.WarehouseTracking.TransferLocation,
							Description = w.WarehouseTracking.Description,
							Status = w.WarehouseTracking.Status,
							ExpectedReturnDate = w.WarehouseTracking.ExpectedReturnDate,
							ActualReturnDate = w.WarehouseTracking.ActualReturnDate,
							EntryDate = w.WarehouseTracking.EntryDate,
							CreatedAt = w.WarehouseTracking.CreatedAt,
							UpdatedAt = w.WarehouseTracking.UpdatedAt,
							CreatedBy = w.WarehouseTracking.CreatedBy,
							UpdatedBy = w.WarehouseTracking.UpdatedBy,
							Supplier = w.WarehouseTracking.Supplier,
							WarehouseTrackingInventory = w.WarehouseTracking.WarehouseTrackingInventory
						},
						Category = w.Category,
						Condition = w.Condition
					});
			if (existingEntity == null)
			{
				// Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "warehouse tracking detail" : "chi tiết dữ liệu nhập kho"));
			}
			
			// Map to dto
			var detailDto = _mapper.Map<WarehouseTrackingDetailDto>(existingEntity);
			// Convert to library item detail
			var libItemDetail = detailDto.LibraryItem?.ToLibraryItemDetailDto();
			// Assign detail's item to null
			detailDto.LibraryItem = null;
			
			// Read success
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), new
				{
					WarehouseTrackingDetail = detailDto,
					LibraryItem = libItemDetail
				});
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
			    // Check exist any cataloged item
			    if (existingEntity.LibraryItemId != null)
			    {
				    // Cannot process update as exist item has been cataloged
				    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0012,
					    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0012));
			    }
			    
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
			    // Add error
			    customErrors.Add(
				    StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
				    [isEng 
					    ? $"ISBN '{dto.Isbn}' already exist in warehouse tracking detail" 
					    : $"Mã ISBN '{dto.Isbn}' đã tồn tại trong chi tiết theo dõi kho"
				    ]);
		    }
		    
		    // Check whether reason change with transaction type
		    if (!Equals(existingEntity.StockTransactionType, dto.StockTransactionType))
		    {
			    // Initialize collection of transaction type
			    var transactionTypes = new List<StockTransactionType>();
			    // Check for invalid detail tracking type
			    var transactionTypeRelations = WarehouseTrackingUtils.TransactionTypeRelations;
			    switch (existingEntity.WarehouseTracking.TrackingType)
			    {
				    case TrackingType.StockIn:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.StockIn, out transactionTypes);
					    break;
				    case TrackingType.StockOut:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.StockOut, out transactionTypes);
					    break;
				    case TrackingType.Transfer:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.Transfer, out transactionTypes);
					    break;
			    }
			    
		        if (transactionTypes == null || !transactionTypes.Any())
	            {
	                // Add error
	                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
		                key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
		                msg: isEng 
			                ? $"Not found any appropriate stock transaction type for tracking type '{existingEntity.WarehouseTracking.TrackingType.ToString()}'" 
			                : "Không tìm thấy loại biến động kho phù hợp");
	            }
	            else
	            {
	                // Convert to str collection
	                var transactionList = transactionTypes.Select(x => x.ToString()).ToList();
	                // Convert record transaction type to str
	                var recordTransactionTypeStr = dto.StockTransactionType.ToString();
	                
	                if (!transactionList.Contains(recordTransactionTypeStr))
	                {
		                // Add error
		                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
			                msg: isEng
				                ? $"'${recordTransactionTypeStr} is invalid. Transaction types must include in '{String.Join(",", transactionList)}'"
				                : $"Loại biến động kho yêu cầu '{String.Join(",", transactionList)}'");
	                }
	            }
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
		    existingEntity.StockTransactionType = dto.StockTransactionType;
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

    public async Task<IServiceResult> GetRangeBarcodeByIdAsync(int trackingDetailId)
    {
	    try
	    {
		    // Build spec
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(wd => wd.TrackingDetailId == trackingDetailId);
		    // Apply include
		    baseSpec.ApplyInclude(q => q.Include(w => w.Category));
			// Check exist tracking detail
			var trackingDetailDto = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
			if (trackingDetailDto == null ||
			    string.IsNullOrEmpty(trackingDetailDto.BarcodeRangeFrom) ||
			    string.IsNullOrEmpty(trackingDetailDto.BarcodeRangeTo))
			{
				// Not found or empty
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					new List<string>());
			}
			
			// Extract num from
			var numFrom = StringUtils.ExtractNumber(
				input: trackingDetailDto.BarcodeRangeFrom, 
				prefix: trackingDetailDto.Category.Prefix, 
				length: _appSettings.InstanceBarcodeNumLength);
			// Extract num to
			var numTo = StringUtils.ExtractNumber(
				input: trackingDetailDto.BarcodeRangeTo, 
				prefix: trackingDetailDto.Category.Prefix, 
				length: _appSettings.InstanceBarcodeNumLength);

			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				// Generate range barcode
				StringUtils.AutoCompleteBarcode(
						prefix: trackingDetailDto.Category.Prefix, 
						length: _appSettings.InstanceBarcodeNumLength, 
						min: numFrom, max: numTo));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process generate range barcode by tracking detail id");
	    }
    }
    
    public async Task<IServiceResult> GetLatestBarcodeByCategoryIdAsync(int categoryId)
    {
	    try
	    {
			// Build spec
			var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(x => x.CategoryId == categoryId);
			// Retrieve with spec and selector
			var barcodes = (await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
				.GetAllWithSpecAndSelectorAsync(baseSpec, selector: w => w.BarcodeRangeTo)).ToList();
			if (barcodes.Any())
			{
				// Retrieve category
				var category = (await _cateSvc.GetByIdAsync(categoryId)).Data as CategoryDto;
				if (category != null)
				{
					// Retrieve largest barcode
					var latestBarcodeNum = barcodes
						.Select(barcode => StringUtils.ExtractNumber(
							input: barcode ?? "0",
							prefix: category.Prefix,
							length: _appSettings.InstanceBarcodeNumLength))
						.OrderByDescending(num => num)
						.FirstOrDefault();
					
					if (latestBarcodeNum != -1)
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0002,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
							// Convert back to string
							StringUtils.AutoCompleteBarcode(
									prefix: category.Prefix, 
									length: _appSettings.InstanceBarcodeNumLength, 
									number: latestBarcodeNum));
					}
				}
			}
			
			// Not found or empty
			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get latest barcode of warehouse tracking detail by category");
	    }
    }
    
    public async Task<IServiceResult> UpdateRangeBarcodeRegistrationAsync(int trackingId, List<int> whDetailIds)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Check exist warehouse tracking
		    var trackingSpec = new BaseSpecification<WarehouseTracking>(x => x.TrackingId == trackingId);
		    // Apply include 
		    trackingSpec.ApplyInclude(q => q.Include(w => w.WarehouseTrackingInventory));
		    var trackingDto = (await _trackingSvc.GetWithSpecAsync(trackingSpec)).Data as WarehouseTrackingDto;
		    if (trackingDto == null || trackingDto.WarehouseTrackingInventory == null!)
		    {
			    // Not found {0}
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "lịch sử theo dõi kho"));
		    }
		    else
		    {
			    // Check for change detail of completed or cancelled warehouse tracking 
			    if (trackingDto.Status == WarehouseTrackingStatus.Completed ||
			        trackingDto.Status == WarehouseTrackingStatus.Cancelled)
			    {
				    // Msg: Cannot change data as warehouse tracking was completed or cancelled
				    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0009,
					    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0009));
			    }
		    }
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(wd =>
			    wd.TrackingId == trackingId && whDetailIds.Contains(wd.TrackingDetailId) && !wd.HasGlueBarcode);
		    // Apply include 
		    baseSpec.ApplyInclude(q => q
			    .Include(wd => wd.Category)
			    .Include(wd => wd.WarehouseTracking)
					.ThenInclude(w => w.WarehouseTrackingInventory)
		    );
		    // Initialize library item that contains collection of instances
		    var libItems = new List<LibraryItemDto>();
		    // Retrieve all data with spec
		    var entities = (await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
			    .GetAllWithSpecAsync(baseSpec)).ToList();
		    if (entities.Any())
		    {
			    // Try to extract warehouse tracking inventory from first ele
			    var whTrackingInven = trackingDto.WarehouseTrackingInventory;
			    
			    // Initialize custom errors
				var customErrors = new Dictionary<string, string[]>();
				
			    // Iterate each wh detail to check whether it has registered barcode
			    for (int i = 0; i < entities.Count; ++i)
			    {
				    var whDetail = entities[i];
				    
				    // Validate stock transaction type to check whether allowing to register unique barcode or not
				    if (whDetail.StockTransactionType != StockTransactionType.New &&
				        whDetail.StockTransactionType != StockTransactionType.Additional)
				    {
					    // Msg: Stock transaction type {0} is not valid to registering unique barcode for item
					    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0019);
					    // Add error 
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"ids[{i}]",
						    msg: StringUtils.Format(errMsg, isEng
								? whDetail.StockTransactionType.ToString()
								: whDetail.StockTransactionType.GetDescription()));
				    }
				    
				    // Check whether warehouse tracking detail contains cataloged item or not
				    if (whDetail.LibraryItemId == null)
				    {
					    // Add error 
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"ids[{i}]",
						    // Msg: Cannot register unique barcode for warehouse tracking detail as it has not been cataloged yet
						    msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0018));
				    }
				    
				    // Has already registered barcode
				    if (whDetail.HasGlueBarcode) 
				    {
						// Add error 
						customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							key: $"ids[{i}]",
							// Msg: Warehouse tracking detail has already been registered unique barcode
							msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0017));
				    }
				    
				    // Check exist barcode range and valid barcode range
				    var numFrom = 0;
				    var numTo = 0;
				    if (!string.IsNullOrEmpty(whDetail.BarcodeRangeFrom) &&
				        !string.IsNullOrEmpty(whDetail.BarcodeRangeTo))
				    {
					    // Try to extract from-to num
					    numFrom = StringUtils.ExtractNumber(
						    input: whDetail.BarcodeRangeFrom,
						    prefix: whDetail.Category.Prefix,
						    length: _appSettings.InstanceBarcodeNumLength);
					    numTo = StringUtils.ExtractNumber(
						    input: whDetail.BarcodeRangeTo,
						    prefix: whDetail.Category.Prefix,
						    length: _appSettings.InstanceBarcodeNumLength);

					    if (numFrom == -1 || numTo == -1)
					    {
						    // Add error 
                            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                                key: $"ids[{i}]",
                                msg: isEng 
	                                ? "Invalid unique registration barcode range"
	                                : "Khoảng đăng ký cá biệt của tài liệu nhập kho không hợp lệ");
					    }
				    }
				    else
				    {
					    // Add error 
                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
                        	key: $"ids[{i}]",
                        	// Msg: Not found range unique registration barcode. Please update to continue
                        	msg: await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0020));
				    }
				    
				    // Process update inventory
				    if (numFrom != 0 && numTo != 0)
				    {
					    // Increase cataloged instance item 
					    whTrackingInven.TotalCatalogedInstanceItem += numTo - numFrom + 1;
					    // Update wh detail status
					    whDetail.HasGlueBarcode = true;
				    }
				    
				    // Process update whDetail status
				    await _unitOfWork.Repository<WarehouseTrackingDetail, int>().UpdateAsync(whDetail);
				    
				    // Initialize list barcodes based on num from-to 
				    var rangeBarcodes = StringUtils.AutoCompleteBarcode(
					    prefix: whDetail.Category.Prefix,
					    length: _appSettings.InstanceBarcodeNumLength,
					    min: numFrom,
					    max: numTo
				    );
				    // Add lib item to process add range instances
				    libItems.Add(new LibraryItemDto()
				    {
					    LibraryItemId = whDetail.LibraryItemId ?? 0,
					    LibraryItemInstances = rangeBarcodes.Select(barcode => new LibraryItemInstanceDto()
					    {
						    // Barcode
						    Barcode = barcode,
						    // Default status
						    Status = nameof(LibraryItemInstanceStatus.OutOfShelf),
						    // Initialize default condition history for item
						    LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
						    {
							    new()
							    {
								    ConditionId = whDetail.ConditionId
							    }
						    }
					    }).ToList()
				    });
			    }
			    
			    // Check whether invoke any error
			    if(customErrors.Any()) throw new UnprocessableEntityException("Invalid Data", customErrors);
			    
		    }
		    
		    // Process update warehouse tracking inventory without save
		    await _trackingSvc.UpdateInventoryWithoutSaveChanges(trackingId, _mapper.Map<WarehouseTrackingDto>(trackingDto));
		    // Process add library item instances to item without save
		    await _itemSvc.AddRangeInstancesWithoutSaveChangesAsync(libItems);
		    // Save DB
		    var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
		    if (isSaved)
		    {
			    // Msg: Total {0} warehouse tracking detail has been registered unique barcode success
			    var successMsg = await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Success0001);
				return new ServiceResult(ResultCodeConst.WarehouseTracking_Success0001,
					StringUtils.Format(successMsg, entities.Count.ToString()));    
		    }

		    // Msg: Failed to register unique barcode for warehouse tracking detail
		    return new ServiceResult(ResultCodeConst.WarehouseTracking_Fail0001,
			    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Fail0001));

		    // Msg: Failed to update
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
		    throw new Exception("Error invoke when process update range warehouse tracking detail barcode registration");
	    }
    }
    
    public async Task<IServiceResult> UpdateItemFromInternalAsync(int trackingDetailId, int libraryItemId)
    {
	    try
	    {
            // Build spec 
            var baseSpec = new BaseSpecification<WarehouseTrackingDetail>(wd => wd.TrackingDetailId == trackingDetailId);
            // Apply include
            baseSpec.ApplyInclude(q => q
	            .Include(wd => wd.WarehouseTracking)
					.ThenInclude(w => w.WarehouseTrackingInventory)
            );
		    // Retrieve by id
            var existingEntity =
	            await _unitOfWork.Repository<WarehouseTrackingDetail, int>().GetWithSpecAsync(baseSpec);
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
	            
	            // Process update warehouse tracking inventory
	            existingEntity.WarehouseTracking.WarehouseTrackingInventory.TotalCatalogedItem++;
	            existingEntity.WarehouseTracking.WarehouseTrackingInventory.TotalCatalogedInstanceItem += existingEntity.ItemTotal;
	            // Mark as has glue barcode
	            existingEntity.HasGlueBarcode = true;
            }
			
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<WarehouseTrackingDetail, int>().HasChanges(existingEntity))
            {
	            // Update success
	            return new ServiceResult(ResultCodeConst.SYS_Success0003,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Update inventory total
            // Get inventory by library item id
            var getInventoryRes = await _inventorySvc.GetWithSpecAsync(
	            new BaseSpecification<LibraryItemInventory>(
		            x => x.LibraryItemId == libraryItemId), tracked: false);
            if (getInventoryRes.Data is LibraryItemInventoryDto inventoryDto) // Get data success
            {
	            // Set relations to null
	            inventoryDto.LibraryItem = null!;
	            // Update total
	            inventoryDto.TotalUnits += existingEntity.ItemTotal;

	            // Update without save
	            await _inventorySvc.UpdateWithoutSaveChangesAsync(inventoryDto);
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
            
            // Not allow to update item when tracking type is not StockIn
            if (existingEntity.WarehouseTracking.TrackingType != TrackingType.StockIn)
            {
	            // Msg: Cannot change item when tracking type is stock out or transfer
	            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0016,
		            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0016));
            }
            
            // Check exist library item
            var libraryItemDto = (await _itemSvc.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
            if (libraryItemDto == null)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"), false);
            }
            
            // Check whether warehouse tracking detail has already been placed in other item
            if (await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(
	                w => 
		                w.TrackingId != existingEntity.TrackingId &&
		                w.LibraryItemId == libraryItemDto.LibraryItemId))
            {
	            // Msg: Warehouse tracking detail has already been in other item
	            return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0013,
		            await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0013));
            }
            
            // Check for item change
            if (!Equals(existingEntity.LibraryItemId, libraryItemId))
            {
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
		    var trackingDto = (await _trackingSvc.GetByIdAsync(trackingId)).Data as WarehouseTrackingDto;
		    if (trackingDto == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
		    }
		    else
		    {
			    // Check for change detail of completed or cancelled warehouse tracking 
			    if (trackingDto.Status == WarehouseTrackingStatus.Completed ||
			        trackingDto.Status == WarehouseTrackingStatus.Cancelled)
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
		    
		    // Check exist category
		    var categoryDto = (await _cateSvc.GetByIdAsync(dto.CategoryId)).Data as CategoryDto;
		    if (categoryDto == null)
		    {
			    // Msg: Not found {0}
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    // Add error
			    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.CategoryId)),
				    msg: StringUtils.Format(msg, isEng ? "item category" : "phân loại tài liệu"));
		    }
		    
		    // Check exist condition
		    var isExistCondition = (await _conditionSvc.AnyAsync(c => c.ConditionId == dto.ConditionId)).Data is true;
		    if (!isExistCondition)
		    {
				// Msg: Not found {0}
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ConditionId)),
					msg: StringUtils.Format(msg, isEng ? "item condition" : "tình trạng tài liệu"));
		    }
			
		    // Check exist ISBN
		    var isIsbnExist = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().AnyAsync(w =>
			    Equals(dto.Isbn, w.Isbn) && w.TrackingId == trackingDto.TrackingId);
		    // Not allow duplicate ISBN in the same warehouse tracking
		    if(isIsbnExist && trackingDto.TrackingType == TrackingType.StockIn) 
		    {
			    // Add error
			    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
				    msg: isEng
					    ? $"ISBN '{dto.Isbn}' already exist in warehouse tracking {trackingDto.ReceiptNumber}"
					    : $"Mã ISBN '{dto.Isbn}' đã tồn tại trong dữ liệu nhập kho '{trackingDto.ReceiptNumber}'");
		    }
		    // Required exist ISBN when tracking type is stock-out or transfer
		    if(!isIsbnExist && (trackingDto.TrackingType == TrackingType.StockOut || trackingDto.TrackingType == TrackingType.Transfer)) 
		    {
			    // Add error
			    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
				    msg: isEng
					    ? $"ISBN '{dto.Isbn}' must exist when tracking type is stock out or transfer"
					    : $"Mã ISBN '{dto.Isbn}' không tồn tại. Yêu cầu mã ISBN của tài liệu đã được biên mục khi xuất kho hoặc trao đổi");
		    }
		    
			// Initialize field to check already announcement of stock transaction type
			var isAnnounceTransType = false;
		    // Set default library item (if null or equals 0)
		    dto.LibraryItemId ??= 0;
		    // Check whether create warehouse tracking detail with item
		    if (dto.LibraryItemId > 0 && dto.LibraryItem != null)
		    {
			    // Mark as fail to create
			    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
		    }
		    
		    // Generate barcode range based on registered warehouse tracking detail
		    var barcodeRangeRes = (await _itemInstanceSvc.GenerateBarcodeRangeAsync(
			    categoryId: dto.CategoryId,
			    totalItem: dto.ItemTotal,
			    skipItem: 0)).Data as GenerateBarcodeRangeResultDto;
		    if (barcodeRangeRes != null && barcodeRangeRes.Barcodes.Any())
		    {
			    if (dto.LibraryItemId > 0 && dto.LibraryItem == null)
			    {
				    // Check transaction type
				    if (dto.StockTransactionType != StockTransactionType.Additional)
				    {
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
						    msg: isEng 
							    ? $"Transaction type must be '{StockTransactionType.Additional.ToString()}' when add tracking detail with item's catalog information" 
							    : $"Loại biến động kho cần chuyển sang '{StockTransactionType.Additional.GetDescription()}' khi tạo dữ liệu nhập kho đi kèm với thông tin biên mục");
					    
					    // Set transaction type announcement to prevent duplicate message
					    isAnnounceTransType = true;
				    }
				    else
				    {
					    // Check exist library item id 
					    var libItemDto = (await _itemSvc.GetByIdAsync(
						    int.Parse(dto.LibraryItemId.ToString() ?? "0"))).Data as LibraryItemDto;
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
					    if (!Equals(dto.CategoryId, libItemDto.CategoryId))
					    {
						    // Add errors
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
							    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.LibraryItem.CategoryId)),
							    msg: isEng
								    ? "Category among warehouse tracking detail with catalog item must be the same"
								    : "Phân loại của dữ liệu nhập kho và tài liệu biên mục đi kèm phải giống nhau");
					    }
					    
					    // Check ISBN match among tracking detail and catalog item
	                    if (!Equals(dto.Isbn, libItemDto.Isbn))
	                    {
	                        // Add errors
	                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
	                            key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn)),
	                            msg: isEng
	                                ? "ISBN is not match with warehouse tracking detail"
	                                : "Mã ISBN không giống với dữ liệu đăng ký nhập kho");			
	                    }
	                    
	                    // Check unit price match among tracking detail and catalog item
	                    if (!Equals(dto.UnitPrice, libItemDto.EstimatedPrice))
	                    {
	                        // Add errors
	                        customErrors = DictionaryUtils.AddOrUpdate(customErrors,
	                            key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.LibraryItem.EstimatedPrice)),
	                            msg: isEng
	                                ? "Estimated price is not match with warehouse tracking detail"
	                                : "Giá tiền tài liệu không giống với dữ liệu đăng ký nhập kho");	
	                    }
					    
	                    /* Do not create any libary item instance when create warehouse tracking detail
	                    // Generate list of instance with default condition history
	                    List<LibraryItemInstanceDto> instances = new();
	                    foreach (var barcode in barcodeRangeRes.Barcodes)
	                    {
		                    instances.Add(new ()
		                    {
			                    Barcode = barcode,
								    
			                    // Default condition
			                    LibraryItemConditionHistories = new List<LibraryItemConditionHistoryDto>()
			                    {
				                    new()
				                    {
					                    ConditionId = dto.ConditionId
				                    }
			                    }
		                    });
	                    }
						    
	                    // Process add range instance to library item
	                    var addRes = await _itemInstanceSvc.AddRangeToLibraryItemAsync(
		                    int.Parse(dto.LibraryItemId.ToString() ?? "0"), instances);
	                    if (addRes.ResultCode != ResultCodeConst.SYS_Success0001)
	                    {
		                    // Log Error
		                    if(addRes.Message != null) _logger.Warning(addRes.Message);
							    
		                    // Mark as fail to create
		                    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
		                    var customMsg = isEng
			                    ? "Cannot add range library item instances to library item"
			                    : "Lưu danh sách đăng ký tài liệu nhập thêm thất bại";
		                    return new ServiceResult(ResultCodeConst.SYS_Fail0001, $"{msg}.{customMsg}");
	                    }
	                    
	                    */
				    }
			    }
			    else if (dto.LibraryItemId == 0 && dto.LibraryItem != null)
			    {
				    // Declare library item
				    var libItem = dto.LibraryItem;
					// Add validation prefix 
					var validationPrefix = StringUtils.ToCamelCase(nameof(LibraryItem));
				    
				    // Check transaction type
				    if (dto.StockTransactionType != StockTransactionType.New)
				    {
					    // Add error
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
						    msg: isEng 
								? $"Transaction type must be '{StockTransactionType.New.ToString()}' when add tracking detail with item's catalog information" 
								: $"Loại biến động kho cần chuyển sang '{StockTransactionType.New.GetDescription()}' khi tạo dữ liệu nhập kho đi kèm với thông tin biên mục");
					    
					    // Set transaction type announcement to prevent duplicate message
					    isAnnounceTransType = true;
				    }
				    
				    // Validate library item
				    var libItemValidateRes = await ValidatorExtensions.ValidateAsync(dto.LibraryItem);
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
									key: $"{validationPrefix}.{error.Key}",
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
				    var countAuthorResult = await _authorSvc.CountAsync(
					    new BaseSpecification<Author>(ct => authorIds.Contains(ct.AuthorId)));
				    // Check exist any author not being counted
				    if (int.TryParse(countAuthorResult.Data?.ToString(), out var totalAuthor) // Parse result to integer
				        && totalAuthor != authorIds.Count) // Not exist 1-many author
				    {
					    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0001,
						    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0001));
				    }
				    
				    // Check same category with warehouse tracking
				    if (!Equals(dto.CategoryId, libItem.CategoryId))
				    {
					    // Add errors
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: $"{validationPrefix}.{StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.CategoryId))}",
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
						    isImageOnCloud = (await _cloudSvc.IsExistAsync(publicId, FileType.Image)).Data is true;
					    }

					    if (!isImageOnCloud || publicId == null) // Not found image or public id
					    {
						    // Add error
						    customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
							    key: $"{validationPrefix}.{StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.LibraryItem.CoverImage))}",
							    msg: await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
					    }
				    }
				    
				    // Check exist condition id 
				    var isConditionExist = (await _conditionSvc.AnyAsync(c => c.ConditionId == dto.ConditionId)).Data is true;
				    if (!isConditionExist)
				    {
					    // Add error
					    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					    customErrors = DictionaryUtils.AddOrUpdate(customErrors,
						    key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ConditionId)),
						    msg: StringUtils.Format(msg, isEng ? "item condition" : "trình trạng tài liệu"));
				    }
				    
				    /* Do not create any library item instance when create warehouse tracking detail
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
		                            key: $"{validationPrefix}.libraryItemInstances[{j}].barcode",
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
		                                key: $"{validationPrefix}.libraryItemInstances[{j}].barcode",
		                                msg: StringUtils.Format(errMsg, $"'{categoryDto.Prefix}'"));
		                        }
		                        
		                        // Try to validate barcode length
		                        var barcodeNumLength = iInstance.Barcode.Length - categoryDto.Prefix.Length; 
		                        if (barcodeNumLength != _appSettings.InstanceBarcodeNumLength) // Different from threshold value
		                        {
		                            // Add errors
		                            customErrors = DictionaryUtils.AddOrUpdate(customErrors, 
		                                key: $"{validationPrefix}.libraryItemInstances[{j}].barcode",
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
		                        key: $"{validationPrefix}.libraryItemInstances[{j}].barcode",
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
		            if (dto.ItemTotal < listItemInstances.Count || dto.ItemTotal > listItemInstances.Count)
		            {
			            // Add errors
			            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				            key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.ItemTotal)),
				            msg: isEng
					            ? "Total item instance must equal to registered warehouse tracking item total"
					            : "Tổng số lượng bản sao biên mục phải bằng với tổng số lượng tài liệu đăng ký nhập kho");	
		            }
		            
		            */
		            
				    // Check ISBN match among tracking detail and catalog item
		            if (!Equals(dto.Isbn, libItem.Isbn))
		            {
			            // Add errors
			            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				            key: $"{validationPrefix}.{StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.Isbn))}",
				            msg: isEng
					            ? "ISBN is not match with warehouse tracking detail"
					            : "Mã ISBN không giống với dữ liệu đăng ký nhập kho");			
		            }
		            
		            // Check unit price match among tracking detail and catalog item
		            if (!Equals(dto.UnitPrice, libItem.EstimatedPrice))
		            {
			            // Add errors
			            customErrors = DictionaryUtils.AddOrUpdate(customErrors,
				            key: $"{validationPrefix}.{StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.LibraryItem.EstimatedPrice))}",
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
			    }
			    
			    // Assign barcode range to warehouse tracking detail
			    dto.BarcodeRangeFrom = barcodeRangeRes.BarcodeRangeFrom;
			    dto.BarcodeRangeTo = barcodeRangeRes.BarcodeRangeTo;
			    dto.HasGlueBarcode = false;
			    
			    // Initialize collection of transaction type
			    var transactionTypes = new List<StockTransactionType>();
			    // Check for invalid detail tracking type
			    var transactionTypeRelations = WarehouseTrackingUtils.TransactionTypeRelations;
			    switch (trackingDto.TrackingType)
			    {
				    case TrackingType.StockIn:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.StockIn, out transactionTypes);
					    break;
				    case TrackingType.StockOut:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.StockOut, out transactionTypes);
					    break;
				    case TrackingType.Transfer:
					    // Try to retrieve all detail transaction type within stock-in tracking type
					    transactionTypeRelations.TryGetValue(TrackingType.Transfer, out transactionTypes);
					    break;
			    }
			    
		        if (transactionTypes == null || !transactionTypes.Any())
	            {
	                // Add error
	                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
		                key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
		                msg: isEng 
			                ? $"Not found any appropriate stock transaction type for tracking type '{trackingDto.TrackingType.ToString()}'" 
			                : "Không tìm thấy loại biến động kho phù hợp");
	            }
	            else if(!isAnnounceTransType)
	            {
	                // Convert to str collection
	                var transactionList = transactionTypes.Select(x => x.ToString()).ToList();
	                // Convert record transaction type to str
	                var recordTransactionTypeStr = dto.StockTransactionType.ToString();
	                
	                if (!transactionList.Contains(recordTransactionTypeStr))
	                {
		                // Add error
		                customErrors = DictionaryUtils.AddOrUpdate(customErrors,
			                key: StringUtils.ToCamelCase(nameof(WarehouseTrackingDetail.StockTransactionType)),
			                msg: isEng
				                ? $"'${recordTransactionTypeStr} is invalid. Transaction types must include in '{String.Join(", ", transactionList)}'"
				                : $"Loại biến động kho yêu cầu '{String.Join(", ", transactionTypes.Select(t => t.GetDescription()))}'");
	                }
	            }
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
		    
		    // Check whether invoke any error
		    if(customErrors.Any()) throw new UnprocessableEntityException("Invalid data", customErrors);
		    
		    // Update tracking inventory without saving
		    var inventory = trackingDto.WarehouseTrackingInventory;		    
		    
		    // Check whether transaction type is 'New' or 'Additional'
		    if ((dto.LibraryItemId != null && dto.LibraryItemId > 0) || dto.LibraryItem != null)
		    {
			    // Increase inventory cataloged item
			    inventory.TotalCatalogedItem++;
			    inventory.TotalCatalogedInstanceItem += dto.ItemTotal;
		    }
		    // Update default inventory amount
		    inventory.TotalItem++;
		    inventory.TotalInstanceItem += dto.ItemTotal;
		    
		    // Add tracking id to dto 
		    dto.TrackingId = trackingId;
		    // Set null if libraryItemId is zero
		    if (dto.LibraryItemId == 0) dto.LibraryItemId = null;
		    
		    // Process update inventory without save change
		    await _trackingSvc.UpdateInventoryWithoutSaveChanges(trackingId, trackingDto);
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
    
    public async Task<IServiceResult> GetAllByTrackingIdAsync(int trackingId, ISpecification<WarehouseTrackingDetail> spec)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
			   
		    // Check existing tracking id 
		    var trackingDto = (await _trackingSvc.GetByIdAndIncludeInventoryAsync(trackingId)).Data as WarehouseTrackingDto;
		    if (trackingDto == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
		    }
			
		    // Try to parse specification to WarehouseTrackingDetailSpecification
		    var detailSpec = spec as WarehouseTrackingDetailSpecification;
		    // Check if specification is null
		    if (detailSpec == null)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }
		    
		    // Apply include
		    detailSpec.ApplyInclude(q => q
			    .Include(w => w.LibraryItem)
					.ThenInclude(li => li!.LibraryItemInventory)
			    .Include(w => w.Category)
		    );
		    
		    // Add tracking filtering 
		    detailSpec.AddFilter(w => w.TrackingId == trackingId);
		    
		    // Count total library items
		    var totalDetailWithSpec = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().CountAsync(detailSpec);
		    // Count total page
		    var totalPage = (int)Math.Ceiling((double)totalDetailWithSpec / detailSpec.PageSize);

		    // Set pagination to specification after count total warehouse tracking detail
		    if (detailSpec.PageIndex > totalPage
		        || detailSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
		    {
			    detailSpec.PageIndex = 1; // Set default to first page
		    }

		    // Apply pagination
		    detailSpec.ApplyPaging(
			    skip: detailSpec.PageSize * (detailSpec.PageIndex - 1),
			    take: detailSpec.PageSize);
		    
		    // Try to retrieve all data by spec
		    var entities = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
			    .GetAllWithSpecAsync(detailSpec);
		    if (entities.Any())
		    {
			    // Convert to dto
			    var detailDtos = _mapper.Map<List<WarehouseTrackingDetailDto>>(entities);
			    
			    // Get all categories
			    var categoryDtos = (await _cateSvc.GetAllAsync()).Data as List<CategoryDto>;
			    if (categoryDtos == null || !categoryDtos.Any())
			    {
				    // Not found {0}
				    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					    StringUtils.Format(errMsg, isEng 
						    ? "categories to get all warehouse tracking detail" 
						    : "phân loại để lấy thông tin nhập kho"));
			    }
			    
			    // Try to retrieve all current tracking details 
			    var allActualDetails = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
				    .GetAllWithSpecAsync(new BaseSpecification<WarehouseTrackingDetail>(wd => wd.TrackingId == trackingId));
			    // Convert to combined dto 
			    var combinedDto = detailDtos.ToDetailCombinedDto(
				    actualTrackingDetails: _mapper.Map<List<WarehouseTrackingDetailDto>>(allActualDetails),
				    trackingDto: trackingDto,
				    categories: categoryDtos,
				    pageIndex: detailSpec.PageIndex,
				    pageSize: detailSpec.PageSize,
				    totalPage: totalPage,
				    totalActualItem: totalDetailWithSpec);
			    
			    // Get data successfully
			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), combinedDto);
		    }

		    // Not found data or empty
		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
			    new List<WarehouseTrackingDetailCombinedDto>());
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all warehouse details");
	    }
    }

    public async Task<IServiceResult> GetAllNotExistItemByTrackingIdAsync(int trackingId, ISpecification<WarehouseTrackingDetail> spec)
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
		    
		    // Try to parse specification to WarehouseTrackingDetailSpecification
		    var detailSpec = spec as WarehouseTrackingDetailSpecification;
		    // Check if specification is null
		    if (detailSpec == null)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }
		    
            // Add tracking filtering 
            detailSpec.AddFilter(w => w.TrackingId == trackingId && w.LibraryItemId == null && 
                                      w.WarehouseTracking.TrackingType == TrackingType.StockIn); // Retrieve stock-in only
		    
            // Count total library items
            var totalDetailWithSpec = await _unitOfWork.Repository<WarehouseTrackingDetail, int>().CountAsync(detailSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDetailWithSpec / detailSpec.PageSize);

            // Set pagination to specification after count total warehouse tracking detail
            if (detailSpec.PageIndex > totalPage
                || detailSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
	            detailSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            detailSpec.ApplyPaging(
	            skip: detailSpec.PageSize * (detailSpec.PageIndex - 1),
	            take: detailSpec.PageSize);
            
            // Try to retrieve all data by spec
            var entities = await _unitOfWork.Repository<WarehouseTrackingDetail, int>()
                .GetAllWithSpecAsync(detailSpec);
            if (entities.Any())
            {
	            // Convert to dto collection
	            var detailDtos = _mapper.Map<List<WarehouseTrackingDetailDto>>(entities);

	            // Pagination result 
	            var paginationResultDto = new PaginatedResultDto<WarehouseTrackingDetailDto>(detailDtos,
		            detailSpec.PageIndex, detailSpec.PageSize, totalPage, totalDetailWithSpec);

	            // Response with pagination 
	            return new ServiceResult(ResultCodeConst.SYS_Success0002,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
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
	            CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<WarehouseTrackingDetailCsvRecord>(
		            file: file, 
		            config: csvConfig,
		            props: new ExcelProps()
		            {
			            // Header start from row 2-3
			            FromRow = 2,
			            ToRow = 3,
			            // Start from col
			            FromCol = 1,
			            // Start read data index
			            StartRowIndex = 4
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
            
            // Exclude all data without item name
            readResp.Records = readResp.Records.Where(r => !string.IsNullOrEmpty(r.ItemName)).ToList();
            
            // Try to detect wrong data
            var wrongDataErrs = await DetectWrongDataAsync(
	            trackingType: trackingEntity.TrackingType,
	            records: readResp.Records,
	            lang: lang);
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
            
            // Retrieve all existing categories
            var categories = (await _cateSvc.GetAllAsync()).Data as List<CategoryDto>;
            if (categories == null || !categories.Any())
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	                StringUtils.Format(msg, isEng
		                ? "categories to process import"
		                : "phân loại để tiến hành import"));
            }
            
            // Retrieve all existing conditions
            var conditions = (await _conditionSvc.GetAllAsync()).Data as List<LibraryItemConditionDto>;
            if (conditions == null || !conditions.Any())
            {
	            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		            StringUtils.Format(msg, isEng
			            ? "conditions to process import"
			            : "danh sách tình trạng sách để tiến hành import"));
            }
            
            // Initialize list of warehouse tracking detail
            var warehouseTrackingDetails = new List<WarehouseTrackingDetailDto>();
            // Progress import warehouse tracking detail
            foreach (var record in readResp.Records)
            {
	            // Get category
	            var category = categories.First(c =>
		            Equals(c.EnglishName.ToLower(), record.Category.ToLower()) ||
		            Equals(c.VietnameseName.ToLower(), record.Category.ToLower()));
	                
	            // Get condition
	            var condition = conditions.First(c => 
		            Equals(c.EnglishName.ToLower(), record.Condition.ToLower()) || 
		            Equals(c.VietnameseName.ToLower(), record.Condition.ToLower()));
	            
	            // Convert to dto detail
	            var trackingDetailDto = record.ToWarehouseTrackingDetailDto(categories, conditions);
	            // Assign category id
	            trackingDetailDto.CategoryId = category.CategoryId;
	            // Assign condition id
	            trackingDetailDto.ConditionId = condition.ConditionId;
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
		catch(UnprocessableEntityException)
		{
			throw;
		}
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process warehouse tracking detail import");
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
	        
	        // Validate transaction type
		    if (!Enum.TryParse(record.StockTransactionType, true, out TransactionType transactionType))
		    {
			    // Add error
			    rowErrors.Add(isEng 
					? $"Stock transaction type '{record.StockTransactionType}' is invalid" 
					: $"Loại biến động kho '{record.StockTransactionType}' không hợp lệ");
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
			    case TrackingType.Transfer:
				    // Try to retrieve all detail transaction type within stock-in tracking type
				    transactionTypeRelations.TryGetValue(TrackingType.Transfer, out transactionTypes);
				    break;
		    }
		    
	        if (transactionTypes == null || !transactionTypes.Any())
            {
                // Add error
                rowErrors.Add(isEng 
					? $"Not found any appropriate stock transaction type for tracking type '{trackingType.ToString()}'" 
					: "Không tìm thấy loại biến động kho phù hợp");
            }
            else
            {
                // Convert to str collection
                var transactionList = transactionTypes.Select(x => x.ToString()).ToList();
                // Convert record transaction type to str
                var recordTransactionTypeStr = transactionType.ToString();
                
                if (!transactionList.Contains(recordTransactionTypeStr))
                {
            	    // Add error
            	    rowErrors.Add(isEng
            		    ? $"'${recordTransactionTypeStr} is invalid. Transaction types must include in '{String.Join(",", transactionList)}'"
            		    : $"Loại biến động kho yêu cầu '{String.Join(",", transactionList)}'");
                }
            }
	        
    	    // Check exist category
    	    if ((await _cateSvc.AnyAsync(c =>
    		        Equals(c.EnglishName.ToLower(), record.Category.ToLower()) || 
    		        Equals(c.VietnameseName.ToLower(), record.Category.ToLower())
    	        )).Data is false)
    	    {
    		    rowErrors.Add(isEng ? "Category name not exist" : "Tên phân loại không tồn tại");
    	    }
	        
	        // Check exist condition
	        if ((await _conditionSvc.AnyAsync(c =>
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
		        if(isIsbnExist && trackingType == TrackingType.StockIn) // Not allow duplicate ISBN when tracking type is StockIn
		        {
			        rowErrors.Add(isEng 
				        ? $"ISBN '{record.Isbn}' already exist" 
				        : $"Mã ISBN '{record.Isbn}' đã tồn tại");
		        }
		        // Required exist ISBN when tracking type is stock-out or transfer
		        else if(!isIsbnExist && 
		                (trackingType == TrackingType.StockOut || 
		                 trackingType == TrackingType.Transfer)) 
		        {
			        rowErrors.Add(isEng 
				        ? $"ISBN '{record.Isbn}' must exist when tracking type is stock out or transfer" 
				        : $"Mã ISBN '{record.Isbn}' không tồn tại. Yêu cầu mã ISBN của tài liệu đã được biên mục khi xuất kho hoặc trao đổi");
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