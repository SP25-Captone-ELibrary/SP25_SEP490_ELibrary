using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryResourceService : GenericService<LibraryResource, LibraryResourceDto, int>,
    ILibraryResourceService<LibraryResourceDto>
{
	private readonly IEmployeeService<EmployeeDto> _empService;
	private readonly ICloudinaryService _cloudService;
	private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;

	public LibraryResourceService(
		ILibraryItemService<LibraryItemDto> libraryItemService,
		ICloudinaryService cloudService,
	    IEmployeeService<EmployeeDto> empService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _empService = empService;
	    _cloudService = cloudService;
	    _libraryItemService = libraryItemService;
    }

	public override async Task<IServiceResult> GetAllWithSpecAsync(
		ISpecification<LibraryResource> specification, 
		bool tracked = true)
	{
		// Try to parse specification to BookResourceSpecification
        var resourceSpec = specification as LibraryResourceSpecification;
        // Check if specification is null
        if (resourceSpec == null)
        {
        	return new ServiceResult(ResultCodeConst.SYS_Fail0002,
        		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        
        // Count total resource
        var totalResourceWithSpec = await _unitOfWork.Repository<LibraryResource, int>().CountAsync(resourceSpec);
        // Count total page
        var totalPage = (int)Math.Ceiling((double)totalResourceWithSpec / resourceSpec.PageSize);
				
        // Set pagination to specification after count total resource 
        if (resourceSpec.PageIndex > totalPage 
            || resourceSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
        {
	        resourceSpec.PageIndex = 1; // Set default to first page
        }
        
        // Apply pagination
        resourceSpec.ApplyPaging(
	        skip: resourceSpec.PageSize * (resourceSpec.PageIndex - 1), 
	        take: resourceSpec.PageSize);
        
        // Get all with spec
        var entities = await _unitOfWork.Repository<LibraryResource, int>()
	        .GetAllWithSpecAsync(resourceSpec, tracked);
        
        if (entities.Any()) // Exist data
        {
        	// Convert to dto collection 
        	var resourceDtos = _mapper.Map<IEnumerable<LibraryResourceDto>>(entities);
        	
        	// Pagination result 
        	var paginationResultDto = new PaginatedResultDto<LibraryResourceDto>(resourceDtos,
		        resourceSpec.PageIndex, resourceSpec.PageSize, totalPage, totalResourceWithSpec);
        	
        	// Response with pagination 
        	return new ServiceResult(ResultCodeConst.SYS_Success0002, 
        		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
        }
        
        // Not found any data
        return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
	        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
	        // Mapping entities to dto 
	        _mapper.Map<IEnumerable<LibraryResourceDto>>(entities));
	}

	public override async Task<IServiceResult> GetByIdAsync(int id)
	{
		try
		{
			// Determine current system lang
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Build spec query
			var baseSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == id);
			// Apply include
			baseSpec.ApplyInclude(q => q
				// Include digital borrows
				.Include(lr => lr.DigitalBorrows));
			// Retrieve entity with spec
			var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
				.GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
			}
			
			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
            _mapper.Map<LibraryResourceDto>(existingEntity));
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when progress get data");
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
			
			// Build a base specification to filter by ResourceId
			var baseSpec = new BaseSpecification<LibraryResource>(s => s.ResourceId == id);

			// Retrieve resource with specification
			var resourceEntity = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(baseSpec);
			if (resourceEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
			}

			// Check whether resource in the trash bin
			if (!resourceEntity.IsDeleted)
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
			}

			// Process add delete entity
			await _unitOfWork.Repository<LibraryResource, int>().DeleteAsync(id);
			// Save to DB
			if (await _unitOfWork.SaveChangesAsync() > 0)
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

	public async Task<IServiceResult> AddResourceToLibraryItemAsync(int libraryItemId, LibraryResourceDto dto)
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
            	throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Check exist item
            var isItemExist = (await _libraryItemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isItemExist)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		            StringUtils.Format(errMsg, isEng 
					? "item to process add resource" 
					: "tài liệu để thêm mới tài nguyên"));
            }
            else
            {
	            // Check not same publicId and resourceUrl
	            var isDuplicateContent = await _unitOfWork.Repository<LibraryResource, int>().AnyAsync(x =>
		            x.ProviderPublicId == dto.ProviderPublicId || // With specific public id
		            x.ResourceUrl == dto.ResourceUrl); // with specific resource url
	            if (isDuplicateContent) // Not allow to have same resource content
	            {
		            return new ServiceResult(ResultCodeConst.LibraryItem_Warning0003,
			            await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0003));
	            }
	            
	            // Check resource format
	            if (!dto.ResourceUrl.Contains(dto.ProviderPublicId)) // Invalid resource format
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
				}
	            
	            // Get file type
				Enum.TryParse(typeof(FileType), dto.FileFormat, out var fileType);
	            // Check exist resource
	            var checkExistResult = await _cloudService.IsExistAsync(dto.ProviderPublicId, (FileType)fileType!);
	            if (checkExistResult.Data is false) return checkExistResult; // Return when not found resource on cloud
            }
  
            // Generate new library item resource
            var libResource = new LibraryItemResource()
            {
				LibraryItemId = libraryItemId,
				LibraryResource = _mapper.Map<LibraryResource>(dto)
            };
            
            // Process add new entity
            await _unitOfWork.Repository<LibraryItemResource, int>().AddAsync(libResource);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
	            return new ServiceResult(ResultCodeConst.SYS_Success0001, 
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }
            else
            {
	            return new ServiceResult(ResultCodeConst.SYS_Fail0001, 
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add item resource");
        }
	}
	
    public override async Task<IServiceResult> UpdateAsync(int id, LibraryResourceDto dto)
    {
        try
		{
			// Determine lang context
			var lang =
				(SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
					.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Validate inputs using the generic validator
			var validationResult = await ValidatorExtensions.ValidateAsync(dto);
			// Check for valid validations
			if (validationResult != null && !validationResult.IsValid)
			{
				// Convert ValidationResult to ValidationProblemsDetails.Errors
				var errors = validationResult.ToProblemDetails().Errors;
				
				// Check if errors contain specific fields (skip for update)
				if (errors.TryGetValue(StringUtils.ToCamelCase(nameof(LibraryResource.ResourceType)), out _))
				{
					errors.Remove(StringUtils.ToCamelCase(nameof(LibraryResource.ResourceType)));
				}
	                
				if (errors.Keys.Any())
				{
					throw new UnprocessableEntityException("Invalid Validations", errors);
				}
			}
			
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
				.GetWithSpecAsync(new BaseSpecification<LibraryResource>(x => x.ResourceId == id));
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "item resource" : "tài nguyên"));
			}
			
			// Check incorrect update
			if (existingEntity.ProviderPublicId != dto.ProviderPublicId || // Not allow to update provider id
			    existingEntity.FileFormat != dto.FileFormat ||  // Update with other file format
			    !dto.ResourceUrl.Contains(dto.ProviderPublicId)) // Invalid resource url 
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
			}

			// Get file type
			Enum.TryParse(typeof(FileType), dto.FileFormat, out var fileType);
			// Check exist resource
			var checkExistResult = await _cloudService.IsExistAsync(dto.ProviderPublicId, (FileType)fileType!);
			if (checkExistResult.Data is false) return checkExistResult; // Not found resource on cloud
			
			// Process update resource properties
			existingEntity.ResourceTitle = dto.ResourceTitle;
			existingEntity.FileFormat = dto.FileFormat;
			existingEntity.ResourceUrl = dto.ResourceUrl;
			existingEntity.ResourceSize = dto.ResourceSize;
			existingEntity.DefaultBorrowDurationDays = dto.DefaultBorrowDurationDays;
			existingEntity.BorrowPrice = dto.BorrowPrice;
			
			// Progress update when all require passed
			await _unitOfWork.Repository<LibraryResource, int>().UpdateAsync(existingEntity);

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
			throw new Exception("Error invoke when process update item resource");
		}
    }
	
    public async Task<IServiceResult> SoftDeleteAsync(int id)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec query
		    var baseSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == id);
		    // Apply include
		    baseSpec.ApplyInclude(q => q
				// Include digital borrows
			    .Include(lr => lr.DigitalBorrows));
		    // Retrieve entity with spec
		    var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
			    .GetWithSpecAsync(baseSpec);
		    // Check if resource already mark as deleted
		    if (existingEntity == null || existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
		    }
    		
		    // Check whether resource is borrowed
		    var hasConstraint = 
				existingEntity.DigitalBorrows.Any(db => 
					// Any digital borrow is currently activating
					db.Status == BorrowDigitalStatus.Active || // OR
					// Has not expired yet
					db.ExpiryDate.Date > DateTime.Now.Date
				);
		    if (hasConstraint) // Has constraint with other relations
		    {
			    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0008, 
				    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008));
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

		    // Mark as update success
		    return new ServiceResult(ResultCodeConst.SYS_Success0007,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);	
		    throw new Exception("Error invoke when process soft delete resources");	
	    }
    }
    
    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching resource
		    // Build spec
		    var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
		    // Apply include
		    baseSpec.ApplyInclude(q => q
			    // Include digital borrows
			    .Include(lr => lr.DigitalBorrows));
		    // Retrieve all data with spec
		    var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var resourceList = resourceEntities.ToList();
		    if (!resourceList.Any())
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
		    }
		    
		    if (resourceList.Any(x => x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }
		    
		    // Add custom errors
		    var customErrs = new Dictionary<string, string[]>();
		    // Progress update deleted status to true
		    for (int i = 0; i < resourceList.Count; ++i)
		    {
			    var src = resourceList[i];
			    
			    // Check whether resource is borrowed or not 
			    var hasConstraint = 
				    src.DigitalBorrows.Any(db => 
                		// Any digital borrow is currently activating
                		db.Status == BorrowDigitalStatus.Active || // OR
		                // Has not expired yet
		                db.ExpiryDate.Date > DateTime.Now.Date
                	);
                if (hasConstraint) // Has constraint with other relations
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
		    
		    
		    // Progress update deleted status to true
		    resourceList.ForEach(x => x.IsDeleted = true);
			
		    // Save changes to DB
		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
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
		    throw new Exception("Error invoke when remove range resource");
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
		    
		    // Check exist resource
		    var existingEntity = await _unitOfWork.Repository<LibraryResource, int>().GetByIdAsync(id);
		    // Check if resource already mark as deleted
		    if (existingEntity == null || !existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
		    }

		    // Update delete status
		    existingEntity.IsDeleted = false;
				
		    // Save changes to DB
		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
		    if (rowsAffected == 0)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0009,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
		    }

		    // Mark as update success
		    return new ServiceResult(ResultCodeConst.SYS_Success0009,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);	
		    throw new Exception("Error invoke when process undo delete resource");	
	    }
    }
    
    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
        try
        {
    	    // Get all matching resource 
    	    // Build spec
    	    var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
    	    var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
    		    .GetAllWithSpecAsync(baseSpec);
    	    // Check if any data already soft delete
    	    var resourceList = resourceEntities.ToList();
    	    if (resourceList.Any(x => !x.IsDeleted))
    	    {
    		    return new ServiceResult(ResultCodeConst.SYS_Fail0009,
    			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
    	    }
    	    
    	    // Progress undo deleted status to false
            resourceList.ForEach(x => x.IsDeleted = false);
    	            
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0009,
	                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
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

    public async Task<IServiceResult> DeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching resource 
		    // Build spec
		    var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
		    var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var resourceList = resourceEntities.ToList();
		    if (resourceList.Any(x => !x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Process delete range
		    await _unitOfWork.Repository<LibraryResource, int>().DeleteRangeAsync(ids);
		    // Save to DB
		    if (await _unitOfWork.SaveChangesAsync() > 0)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
			    return new ServiceResult(ResultCodeConst.SYS_Success0008,
				    StringUtils.Format(msg, resourceList.Count.ToString()), true);
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
		    throw new Exception("Error invoke when process delete range resource");
	    }
    }
}