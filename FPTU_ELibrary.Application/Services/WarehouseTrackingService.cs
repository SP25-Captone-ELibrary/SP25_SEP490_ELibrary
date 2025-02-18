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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class WarehouseTrackingService : GenericService<WarehouseTracking, WarehouseTrackingDto, int>, 
    IWarehouseTrackingService<WarehouseTrackingDto>
{
    private readonly ISupplierService<SupplierDto> _supplierService;
    private readonly ICategoryService<CategoryDto> _cateService;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionService;

    public WarehouseTrackingService(
        ISupplierService<SupplierDto> supplierService,
        ICategoryService<CategoryDto> cateService,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _cateService = cateService;
        _supplierService = supplierService;
        _conditionService = conditionService;
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
	    try
	    {
			// Build specification
			var baseSpec = new BaseSpecification<WarehouseTracking>(w => w.TrackingId == id);
			// Apply include
			baseSpec.ApplyInclude(q => q
				.Include(w => w.Supplier)
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
		    
		    // Check whether supplier change
		    if (!Equals(existingEntity.SupplierId, dto.SupplierId))
		    {
			    // Check exist supplier
			    if ((await _supplierService.AnyAsync(x => x.SupplierId == dto.SupplierId)).Data is false)
			    {
				    // Not found
				    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					    StringUtils.Format(msg, isEng
						    ? "supplier to process update warehouse tracking"
						    : "bên cung cấp để sửa thông tin theo dõi kho"));
			    }
		    }

		    // Check status
		    if (existingEntity.Status != WarehouseTrackingStatus.Draft)
		    {
			    // Not allow to perform update when status is not Draft
			    return new ServiceResult(ResultCodeConst.WarehouseTracking_Warning0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.WarehouseTracking_Warning0002));
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
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update warehouse tracking");
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

    public async Task<IServiceResult> CreateAndImportDetailsAsync(
        WarehouseTrackingDto dto,
        IFormFile? trackingDetailsFile,
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

            // Check exist supplier 
            var isSupplierExist = (await _supplierService.AnyAsync(s => s.SupplierId == dto.SupplierId)).Data is true;
            if (!isSupplierExist)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "supplier" : "bên liên quan"));
            }

            // Generate receipt number
            var receiptNum = $"WT{StringUtils.GenerateRandomCodeDigits(8)}";
            // Add necessary fields
            dto.ReceiptNumber = receiptNum;
            dto.Status = WarehouseTrackingStatus.Draft;

            // Additional message
            var additionalMsg = string.Empty;
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
                        file: trackingDetailsFile, 
                        config: csvConfig,
                        props: new ExcelHeaderProps()
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
                
                var categories = (await _cateService.GetAllAsync()).Data as List<CategoryDto>;
                if (categories == null || !categories.Any())
                {
	                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		                StringUtils.Format(msg, isEng
			                ? "categories to process import"
			                : "phân loại để tiến hành import"));
                }

                var conditions = (await _conditionService.GetAllAsync()).Data as List<LibraryItemConditionDto>;
                if (conditions == null || !conditions.Any())
                {
	                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		                StringUtils.Format(msg, isEng
			                ? "conditions to process import"
			                : "danh sách tình trạng sách để tiến hành import"));
                }
                
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
	                var trackingDetailDto = record.ToWarehouseTrackingDetailDto();
	                // Assign category id
	                trackingDetailDto.CategoryId = category.CategoryId;
	                // Assign condition id
	                trackingDetailDto.ConditionId = condition.ConditionId;
	                // Add to warehouse tracking
	                dto.WarehouseTrackingDetails.Add(trackingDetailDto);
                }                
            }

            // Progress add new warehouse tracking 
            await _unitOfWork.Repository<WarehouseTracking, int>().AddAsync(_mapper.Map<WarehouseTracking>(dto));
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
	            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
	            if(dto.WarehouseTrackingDetails.Any())
				{
					var customMsg = string.IsNullOrEmpty(additionalMsg)
                        ? isEng 
	                        ? $"total {dto.WarehouseTrackingDetails.Count} warehouse tracking details have been saved"
	                        : $"tổng {dto.WarehouseTrackingDetails.Count} thông tin tài liệu đã được thêm"
                        : isEng
							? $"total {dto.WarehouseTrackingDetails.Count} warehouse tracking details have been saved, {additionalMsg}"
							: $"tổng {dto.WarehouseTrackingDetails.Count} thông tin tài liệu đã được thêm, {additionalMsg}";
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