using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
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

public class BookResourceService : GenericService<BookResource, BookResourceDto, int>,
    IBookResourceService<BookResourceDto>
{
	private readonly IEmployeeService<EmployeeDto> _empService;
	private readonly IBookService<BookDto> _bookService;
	private readonly ICloudinaryService _cloudService;

	public BookResourceService(
		ICloudinaryService cloudService,
		IBookService<BookDto> bookService,
	    IEmployeeService<EmployeeDto> empService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _empService = empService;
	    _bookService = bookService;
	    _cloudService = cloudService;
    }

	public override async Task<IServiceResult> GetAllWithSpecAsync(
		ISpecification<BookResource> specification, 
		bool tracked = true)
	{
		// Try to parse specification to BookResourceSpecification
        var bookResourceSpec = specification as BookResourceSpecification;
        // Check if specification is null
        if (bookResourceSpec == null)
        {
        	return new ServiceResult(ResultCodeConst.SYS_Fail0002,
        		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        
        // Count total book resource
        var totalEmployeeWithSpec = await _unitOfWork.Repository<BookResource, int>().CountAsync(bookResourceSpec);
        // Count total page
        var totalPage = (int)Math.Ceiling((double)totalEmployeeWithSpec / bookResourceSpec.PageSize);
				
        // Set pagination to specification after count total book resource 
        if (bookResourceSpec.PageIndex > totalPage 
            || bookResourceSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
        {
	        bookResourceSpec.PageIndex = 1; // Set default to first page
        }
        
        // Apply pagination
        bookResourceSpec.ApplyPaging(
	        skip: bookResourceSpec.PageSize * (bookResourceSpec.PageIndex - 1), 
	        take: bookResourceSpec.PageSize);
        
        // Get all with spec
        var entities = await _unitOfWork.Repository<BookResource, int>()
	        .GetAllWithSpecAsync(bookResourceSpec, tracked);
        
        if (entities.Any()) // Exist data
        {
        	// Convert to dto collection 
        	var bookResourceDtos = _mapper.Map<IEnumerable<BookResourceDto>>(entities);
        	
        	// Pagination result 
        	var paginationResultDto = new PaginatedResultDto<BookResourceDto>(bookResourceDtos,
		        bookResourceSpec.PageIndex, bookResourceSpec.PageSize, totalPage, totalEmployeeWithSpec);
        	
        	// Response with pagination 
        	return new ServiceResult(ResultCodeConst.SYS_Success0002, 
        		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
        }
        
        // Not found any data
        return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
	        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
	        // Mapping entities to dto 
	        _mapper.Map<IEnumerable<BookResourceDto>>(entities));
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
			var baseSpec = new BaseSpecification<BookResource>(s => s.ResourceId == id);

			// Retrieve book resource with specification
			var resourceEntity = await _unitOfWork.Repository<BookResource, int>().GetWithSpecAsync(baseSpec);
			if (resourceEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "book resource" : "tài nguyên sách"));
			}

			// Check whether book resource in the trash bin
			if (!resourceEntity.IsDeleted)
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
			}

			// Process add delete entity
			await _unitOfWork.Repository<BookResource, int>().DeleteAsync(id);
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

	public async Task<IServiceResult> AddBookResourceToBookAsync(
		int bookId, BookResourceDto dto, string byEmail)
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
            
            // Retrieve create by employee
            var employeeCreate = (await _empService.GetByEmailAsync(byEmail)).Data as EmployeeDto;
            if (employeeCreate == null) // not found create by (employee)
            {
	            // Mark as Forbidden
	            throw new ForbiddenException("Not allow to access");
            }

            // Check exist book 
            var existBookExist = (await _bookService.AnyAsync(x => x.BookId == bookId)).Data is true;
            if (!existBookExist)
            {
	            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		            StringUtils.Format(errMsg, isEng 
					? "book to process add book resource" 
					: "sách để thêm mới tài nguyên"));
            }
            else
            {
	            // Check not same publicId and resourceUrl
	            var isDuplicateContent = await _unitOfWork.Repository<BookResource, int>().AnyAsync(x =>
		            x.ProviderPublicId == dto.ProviderPublicId || // With specific public id
		            x.ResourceUrl == dto.ResourceUrl); // with specific resource url
	            if (isDuplicateContent) // Not allow to have same resource content
	            {
		            return new ServiceResult(ResultCodeConst.Book_Warning0004,
			            await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0004));
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
	            
	            // Update book id property
	            dto.BookId = bookId;
            }
            
            // Process add new entity
            await _unitOfWork.Repository<BookResource, int>().AddAsync(_mapper.Map<BookResource>(dto));
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
        catch (ForbiddenException)
        {
	        throw;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add book resource");
        }
	}
	
    public async Task<IServiceResult> UpdateAsync(int id, BookResourceDto dto, string byEmail)
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
				if (errors.TryGetValue("resourceType", out _)) errors.Remove("resourceType");
	                
				if (errors.Keys.Any())
				{
					throw new UnprocessableEntityException("Invalid Validations", errors);
				}
			}
			
			// Retrieve create by employee
			var employeeUpdate = (await _empService.GetByEmailAsync(byEmail)).Data as EmployeeDto;
			if (employeeUpdate == null) // not found create by (employee)
			{
				// Mark as Forbidden
				throw new ForbiddenException("Not allow to access");
			}
			
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<BookResource, int>()
				.GetWithSpecAsync(new BaseSpecification<BookResource>(x => x.ResourceId == id));
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "book resource" : "tài nguyên sách"));
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
			existingEntity.Provider = dto.Provider;
			existingEntity.ResourceUrl = dto.ResourceUrl;
			existingEntity.ResourceSize = dto.ResourceSize;
			
			// Progress update when all require passed
			await _unitOfWork.Repository<BookResource, int>().UpdateAsync(existingEntity);

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
			throw new Exception("Error invoke when process update book resource");
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
		    
		    // Check exist book resource
		    var existingEntity = await _unitOfWork.Repository<BookResource, int>().GetByIdAsync(id);
		    // Check if book resource already mark as deleted
		    if (existingEntity == null || existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book resource" : "tài nguyên của sách"));
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
		    throw new Exception("Error invoke when process soft delete book resources");	
	    }
    }
    
    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching book resource
		    // Build spec
		    var baseSpec = new BaseSpecification<BookResource>(e => ids.Contains(e.ResourceId));
		    var resourceEntities = await _unitOfWork.Repository<BookResource, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var resourceList = resourceEntities.ToList();
		    if (resourceList.Any(x => x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
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
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when remove range book resource");
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
		    
		    // Check exist book resource
		    var existingEntity = await _unitOfWork.Repository<BookResource, int>().GetByIdAsync(id);
		    // Check if book resource already mark as deleted
		    if (existingEntity == null || !existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book resource" : "tài nguyên của sách"));
		    }

		    // Update delete status
		    existingEntity.IsDeleted = false;
				
		    // Save changes to DB
		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
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
		    throw new Exception("Error invoke when process undo delete book resource");	
	    }
    }
    
    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
        try
        {
    	    // Get all matching book resource 
    	    // Build spec
    	    var baseSpec = new BaseSpecification<BookResource>(e => ids.Contains(e.ResourceId));
    	    var resourceEntities = await _unitOfWork.Repository<BookResource, int>()
    		    .GetAllWithSpecAsync(baseSpec);
    	    // Check if any data already soft delete
    	    var resourceList = resourceEntities.ToList();
    	    if (resourceList.Any(x => !x.IsDeleted))
    	    {
    		    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
    			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
    	    }
    	    
    	    // Progress undo deleted status to false
            resourceList.ForEach(x => x.IsDeleted = false);
    	            
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
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

    public async Task<IServiceResult> DeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching book resource 
		    // Build spec
		    var baseSpec = new BaseSpecification<BookResource>(e => ids.Contains(e.ResourceId));
		    var resourceEntities = await _unitOfWork.Repository<BookResource, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var resourceList = resourceEntities.ToList();
		    if (resourceList.Any(x => !x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Process delete range
		    await _unitOfWork.Repository<BookResource, int>().DeleteRangeAsync(ids);
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
		    throw new Exception("Error invoke when process delete range book resource");
	    }
    }
    
}