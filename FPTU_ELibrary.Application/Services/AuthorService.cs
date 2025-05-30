using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AuthorService : GenericService<Author, AuthorDto, int>, IAuthorService<AuthorDto>
{
	// Lazy services
	private readonly Lazy<ILibraryItemService<LibraryItemDto>> _libItemSvc;
	
    public AuthorService(
	    // Lazy services
		Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,    
	    
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
	    _libItemSvc = libItemSvc;
    }

    public override async Task<IServiceResult> CreateAsync(AuthorDto dto)
    {
	    // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
	        // Determine current system language
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
	        // Check exist author name
	        var authorEntity = await _unitOfWork.Repository<Author, int>().GetWithSpecAsync(
		        new BaseSpecification<Author>(a =>
			        Equals(a.FullName.ToLower(), dto.FullName.ToLower()) && // Same fullname
			        (
				        (a.Nationality == null && dto.Nationality == null) ||
				        (a.Nationality != null && dto.Nationality != null &&
				         string.Equals(a.Nationality.ToLower(), dto.Nationality.ToLower()))
			        ) && // Same nationality
			        (
				        (!a.Dob.HasValue && !dto.Dob.HasValue) ||
				        (a.Dob.HasValue && dto.Dob.HasValue && a.Dob.Value.Date == dto.Dob.Value.Date)
			        ) && // Same Dob
			        (
				        (!a.DateOfDeath.HasValue && !dto.DateOfDeath.HasValue) ||
				        (a.DateOfDeath.HasValue && dto.DateOfDeath.HasValue && a.DateOfDeath.Value.Date == dto.DateOfDeath.Value.Date)
			        ))); // Same Date of death 
	        if (authorEntity != null) // Duplicate name found
	        {
		        // Return error invoked
                return new ServiceResult(ResultCodeConst.SYS_Warning0001,
                	isEng
                		? "Added author name, dob, nationality already match with other author"
                		: "Tên tác giả, ngày sinh, quốc tịch đã tồn tại trong thông tin của tác giả khác");
	        }

	        // Generate author code
	        var latestAuthSpec = new BaseSpecification<Author>();
	        // Add order by
	        latestAuthSpec.AddOrderByDescending(a => a.AuthorId);
	        // Retrieve with spec
			var latestAuthor = await _unitOfWork.Repository<Author, int>().GetWithSpecAsync(latestAuthSpec);
			if (latestAuthor == null)
			{
				// Set default author code
				dto.AuthorCode = StringUtils.AutoCompleteBarcode(prefix: "AUTH", length: 5, number: 1);
			}
			else
			{
				// Extract latest number
				var latestNumber = StringUtils.ExtractNumber(latestAuthor.AuthorCode, prefix: "AUTH", length: 5);
				// Generate new code
				dto.AuthorCode = StringUtils.AutoCompleteBarcode(prefix: "AUTH", length: 5, number: latestNumber + 1);
			}
			
	        // Check invoke any errors
	        if (customErrors.Any())
	        {
		        // Throw invalid data exception
		        throw new UnprocessableEntityException("Invalid data", customErrors);
	        }

	        // Process add new entity
	        await _unitOfWork.Repository<Author, int>().AddAsync(_mapper.Map<Author>(dto));
	        // Save to DB
	        if (await _unitOfWork.SaveChangesAsync() > 0)
	        {
		        serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
		        serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
		        serviceResult.Data = true;
	        }
	        else
	        {
		        serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
		        serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
		        serviceResult.Data = false;
	        }
        }
        catch (UnprocessableEntityException)
        {
	        throw;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }
        
        return serviceResult;
    }
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(
	    ISpecification<Author> specification, 
	    bool tracked = true)
    {
        try
		{
			// Try to parse specification to AuthorSpecification
			var authorSpec = specification as AuthorSpecification;
			// Check if specification is null
			if (authorSpec == null)
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}				
			
			// Count total authors
			var totalAuthorWithSpec = await _unitOfWork.Repository<Author, int>().CountAsync(authorSpec);
			// Count total page
			var totalPage = (int)Math.Ceiling((double)totalAuthorWithSpec / authorSpec.PageSize);
			
			// Set pagination to specification after count total authors 
			if (authorSpec.PageIndex > totalPage 
			    || authorSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
			{
				authorSpec.PageIndex = 1; // Set default to first page
			}
			
			// Apply pagination
			authorSpec.ApplyPaging(
				skip: authorSpec.PageSize * (authorSpec.PageIndex - 1), 
				take: authorSpec.PageSize);
			
			// Get all with spec
			var entities = await _unitOfWork.Repository<Author, int>()
				.GetAllWithSpecAsync(authorSpec, tracked);
			
			if (entities.Any()) // Exist data
			{
				// Convert to dto collection 
				var authorDtos = _mapper.Map<IEnumerable<AuthorDto>>(entities);
				
				// Pagination result 
				var paginationResultDto = new PaginatedResultDto<AuthorDto>(authorDtos,
					authorSpec.PageIndex, authorSpec.PageSize, totalPage, totalAuthorWithSpec);
				
				// Response with pagination 
				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
			}
			
			// Not found any data
			return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
				// Mapping entities to dto 
				_mapper.Map<IEnumerable<AuthorDto>>(entities));
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when progress get all data");
		}
    }

    public async Task<IServiceResult> GetAllByCodesAsync(string[] authorCodes)
    {
	    try
	    {
		    // Build spec 
		    var baseSpec = new BaseSpecification<Author>(a => authorCodes.Contains(a.AuthorCode));
		    // Retrieve all author by code
		    var authorEntities = await _unitOfWork.Repository<Author, int>().GetAllWithSpecAsync(baseSpec);
		    if (authorEntities.Any())
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
				    _mapper.Map<List<AuthorDto>>(authorEntities));
		    }

		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), new List<AuthorDto>());
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when progress get all data");
	    }
    }
    
    public async Task<IServiceResult> GetRelatedAuthorItemsAsync(int authorId, int pageIndex, int pageSize)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve all items of specific author
            // Build spec
            var baseSpec = new BaseSpecification<Author>(li => li.AuthorId == authorId);
            // Enable split query
            baseSpec.EnableSplitQuery();
            // Apply include
            baseSpec.ApplyInclude(q => q
	            .Include(li => li.LibraryItemAuthors)
					.ThenInclude(lia => lia.LibraryItem)
						.ThenInclude(li => li.Category)
            );
            
            // Try to retrieve with spec
            var authorEntity = await _unitOfWork.Repository<Author, int>()
                .GetWithSpecAsync(baseSpec);
            // Check exist item
            if (authorEntity == null)
            {
	            // Response empty
	            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
		            new PaginatedResultDto<LibraryItemDetailDto>(
			            new List<LibraryItemDetailDto>(), 0,0,0,0));
            }
            
            // Extract all author items
            var authorItems = authorEntity.LibraryItemAuthors
                .Select(x => x.LibraryItem)
                .ToList();
            if (authorItems.Any())
            {
                // Convert to dto
                var dtos = _mapper.Map<List<LibraryItemDto>>(authorItems);
                var details = dtos.Select(x => x.ToLibraryItemGroupedDetailDto());
                
                // Pagination
                var totalActualItem = authorItems.Count;
                var totalPage = (int) Math.Ceiling((double)totalActualItem / pageSize);
                
                // Validate pagination fields
                if (pageIndex < 1 || pageIndex > totalPage) pageIndex = 1;
                
                // Apply pagination
                details = details.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryItemDetailDto>(details,
                    pageIndex, pageSize, totalPage, totalActualItem);
                
                // Return the result
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
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
            throw new Exception("Error invoke when progress get related author items");
        }
    }

    public async Task<IServiceResult> GetFirstByLibraryItemIdAsync(int libraryItemId)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
			// Check exist lib item id 
			var isExistItem = (await _libItemSvc.Value.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
			if (!isExistItem)
			{
				// Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
			}
			
			// Build spec
			var libAuthorSpec = new BaseSpecification<LibraryItemAuthor>(lia => lia.LibraryItemId == libraryItemId);
			// Apply include
			libAuthorSpec.ApplyInclude(q => q.Include(lia => lia.Author));
			// Retrieve with spec
			var libAuthor = await _unitOfWork.Repository<LibraryItemAuthor, int>().GetWithSpecAsync(libAuthorSpec);
			
			// Response author information (if any)
			if (libAuthor != null)
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
					_mapper.Map<AuthorDto>(libAuthor.Author));
			}
			
			// Not found {0}
			var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				StringUtils.Format(msg, isEng ? "any author in library item" : "tác giả nào gắn với tài liệu"));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get first author by library item id");
	    }
    }
    
    public override async Task<IServiceResult> UpdateAsync(int id, AuthorDto dto)
    {
	    // Initiate service result
		var serviceResult = new ServiceResult();

		try
		{
			// Determine current system language
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

			// Custom errors
			var customErrors = new Dictionary<string, string[]>();
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<Author, int>().GetByIdAsync(id);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "author" : "tác giả"));
			}
			else
			{
				// Check constraints with other author information
				var otherAuthors = await _unitOfWork.Repository<Author, int>().GetAllWithSpecAsync(
					new BaseSpecification<Author>(a =>
						Equals(a.FullName.ToLower(), dto.FullName.ToLower()) && // Same fullname
						(
							(a.Nationality == null && dto.Nationality == null) ||
							(a.Nationality != null && dto.Nationality != null &&
							 string.Equals(a.Nationality.ToLower(), dto.Nationality.ToLower()))
						) && // Same nationality
						(
							(!a.Dob.HasValue && !dto.Dob.HasValue) ||
							(a.Dob.HasValue && dto.Dob.HasValue && a.Dob.Value.Date == dto.Dob.Value.Date)
						) && // Same dob
						(
							(!a.DateOfDeath.HasValue && !dto.DateOfDeath.HasValue) ||
							(a.DateOfDeath.HasValue && dto.DateOfDeath.HasValue && a.DateOfDeath.Value.Date == dto.DateOfDeath.Value.Date)
						) && // Same date of death
						a.AuthorId != existingEntity.AuthorId // With other authors
					));
				
				if (otherAuthors.Any()) // Same information with other authors
				{
					// Return error invoked
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						isEng
							? "Updated author name, dob, nationality already match with other author"
							: "Tên tác giả, ngày sinh, quốc tịch đã tồn tại trong thông tin của tác giả khác");
				}
			}
			
			// Invoke any errors
            if (customErrors.Any()) 
            {
	            throw new UnprocessableEntityException("Invalid data", customErrors);
            }
            
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			// Update text properties
			existingEntity.AuthorImage = dto.AuthorImage;
			existingEntity.FullName = dto.FullName;
			existingEntity.Biography = dto.Biography;
			existingEntity.Nationality = dto.Nationality;
			existingEntity.Dob = dto.Dob;
			existingEntity.DateOfDeath = dto.DateOfDeath;
			existingEntity.UpdateDate = currentLocalDateTime;

			// Check if there are any differences between the original and the updated entity
			if (!_unitOfWork.Repository<Author, int>().HasChanges(existingEntity))
			{
				serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
				serviceResult.Data = true;
				return serviceResult;
			}

			// Progress update when all require passed
			await _unitOfWork.Repository<Author, int>().UpdateAsync(existingEntity);

			// Save changes to DB
			var rowsAffected = await _unitOfWork.SaveChangesAsync();
			if (rowsAffected == 0)
			{
				serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
				serviceResult.Data = false;
			}

			// Mark as update success
			serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
			serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
			serviceResult.Data = true;
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

		return serviceResult;
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
		    // Determine current system language
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build a base specification to filter by AuthorId
		    var baseSpec = new BaseSpecification<Author>(a => a.AuthorId == id);

		    // Retrieve author with specification
		    var authorEntity = await _unitOfWork.Repository<Author, int>().GetWithSpecAsync(baseSpec);
		    if (authorEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "author" : "tác giả"));
		    }

		    // Check whether author in the trash bin
		    if (!authorEntity.IsDeleted)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Process add delete entity
		    await _unitOfWork.Repository<Author, int>().DeleteAsync(id);
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
	
    public async Task<IServiceResult> GetAuthorDetailByIdAsync(int id)
    {
	    try
	    {
			// Get author by id 
			var author = await _unitOfWork.Repository<Author, int>()
				.GetWithSpecAsync(new BaseSpecification<Author>(q => q.AuthorId == id));
			if (author == null) // Not found author
			{
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}
			
			// Process add author book related details
			// Build spec 
			var spec = new BaseSpecification<Author>(ba => ba.AuthorId == id);
			// Enable split query
			spec.EnableSplitQuery();
		    // Include library item and reviews
		    spec.ApplyInclude(q => q
			    .Include(b => b.LibraryItemAuthors)
					.ThenInclude(be => be.LibraryItem)
						.ThenInclude(li => li.LibraryItemReviews));
		    // Order by RatingValue of item review rate
		    spec.AddOrderBy(q => q.LibraryItemAuthors
			    .SelectMany(x => x.LibraryItem.LibraryItemReviews)
			    .Average(br => br.RatingValue));
		    
			// Get author with specification
			var authorEntity = await _unitOfWork.Repository<Author, int>()
				.GetWithSpecAsync(spec);
			if (authorEntity == null)
			{
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}
			
			// Count author total published items
			var totalAuthorPublishedBook = authorEntity.LibraryItemAuthors.Select(lia => lia.LibraryItem).Count();
			// Count avg item reviews
			var totalBookReviewAvg = authorEntity.LibraryItemAuthors
				.SelectMany(x => x.LibraryItem.LibraryItemReviews)
				.Select(br => br.RatingValue)
				.DefaultIfEmpty(0)
				.Average();
			
			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				new AuthorDetailDto
				{
					Author = _mapper.Map<AuthorDto>(author),
					UserReviews = totalBookReviewAvg,
					TotalPublishedBook = totalAuthorPublishedBook,
					TopReviewedBooks = authorEntity.LibraryItemAuthors.Select(bAuthor => 
						new AuthorTopReviewedBookDto
						{
							LibraryItem = _mapper.Map<LibraryItemDto>(bAuthor.LibraryItem)
						}).Take(5).ToList()
				});
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when progress get data");
	    }
    }
    
    public async Task<IServiceResult> SoftDeleteAsync(int id)
    {
    	try
    	{
    		// Check exist author
    		var existingEntity = await _unitOfWork.Repository<Author, int>().GetByIdAsync(id);
    		// Check if author already mark as deleted
    		if (existingEntity == null || existingEntity.IsDeleted)
    		{
    			var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
    			return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    				StringUtils.Format(errMsg, "author"));
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
    		throw new Exception("Error invoke when process soft delete authors");	
    	}
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching author 
		    // Build spec
		    var baseSpec = new BaseSpecification<Author>(e => ids.Contains(e.AuthorId));
		    var authorEntities = await _unitOfWork.Repository<Author, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var authorList = authorEntities.ToList();
		    if (authorList.Any(x => x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }
		    
		    // Progress update deleted status to true
		    authorList.ForEach(x => x.IsDeleted = true);
			
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
		    throw new Exception("Error invoke when remove range author");
	    }
    }
    
    public async Task<IServiceResult> UndoDeleteAsync(int id)
    {
	    try
	    {
		    // Check exist author
		    var existingEntity = await _unitOfWork.Repository<Author, int>().GetByIdAsync(id);
		    // Check if author already mark as deleted
		    if (existingEntity == null || !existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, "author"));
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
		    throw new Exception("Error invoke when process undo delete author");	
	    }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
	    try
	    {
		    // Get all matching author 
		    // Build spec
		    var baseSpec = new BaseSpecification<Author>(e => ids.Contains(e.AuthorId));
		    var authorEntities = await _unitOfWork.Repository<Author, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var authorList = authorEntities.ToList();
		    if (authorList.Any(x => !x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }
		    
		    // Progress undo deleted status to false
            authorList.ForEach(x => x.IsDeleted = false);
		            
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
		    // Get all matching author 
		    // Build spec
		    var baseSpec = new BaseSpecification<Author>(e => ids.Contains(e.AuthorId));
		    var authorEntities = await _unitOfWork.Repository<Author, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var authorList = authorEntities.ToList();
		    if (authorList.Any(x => !x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Process delete range
		    await _unitOfWork.Repository<Author, int>().DeleteRangeAsync(ids);
		    // Save to DB
		    if (await _unitOfWork.SaveChangesAsync() > 0)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
			    return new ServiceResult(ResultCodeConst.SYS_Success0008,
				    StringUtils.Format(msg, authorList.Count.ToString()), true);
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
		    throw new Exception("Error invoke when process delete range author");
	    }
    }

    public async Task<IServiceResult> ImportAsync(
	    IFormFile? file,
	    DuplicateHandle duplicateHandle,
	    string[]? scanningFields)
    {
	    try
		{
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
			List<AuthorCsvRecord> records =
				CsvUtils.ReadCsvOrExcel<AuthorCsvRecord>(file, csvConfig, null);

			// Determine system lang
			var lang =
				(SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
					.CurrentLanguage);

			// Detect record errors
			var detectResult = await DetectWrongDataAsync(records, scanningFields, (SystemLanguage) lang!);
			if (detectResult.Any())
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
			var detectDuplicateResult = DetectDuplicates(records, scanningFields);
			if (detectDuplicateResult.Any())
			{
				var handleResult = CsvUtils.HandleDuplicates(records, detectDuplicateResult, duplicateHandle, lang);
				// Update records
				records = handleResult.handledRecords;
				// Update msg 
				additionalMsg = handleResult.msg;
			}

			// Convert to author dto collection
			var authorDtos = records.ToAuthorDtosForImport();

			// Progress import data
			await _unitOfWork.Repository<Author, int>().AddRangeAsync(_mapper.Map<List<Author>>(authorDtos));
			// Save to DB
			if (await _unitOfWork.SaveChangesAsync() > 0)
			{
				var respMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
				respMsg = !string.IsNullOrEmpty(additionalMsg)
					? $"{StringUtils.Format(respMsg, authorDtos.Count.ToString())}, {additionalMsg}"
					: StringUtils.Format(respMsg, authorDtos.Count.ToString());
				return new ServiceResult(ResultCodeConst.SYS_Success0005, respMsg, true);
			}

			return new ServiceResult(ResultCodeConst.SYS_Warning0005,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0005), false);
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (TypeConverterException ex)
		{
			var lang =
				(SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
					.CurrentLanguage);
			// Extract row information if available
			var rowNumber = ex.Data.Contains("Row") ? ex.Data["Row"] : "unknown";

			// Generate an appropriate error message
			var errMsg = lang == SystemLanguage.English
				? $"Wrong data type at row {rowNumber}"
				: $"Sai kiểu dữ liệu ở dòng {rowNumber}";

			throw new BadRequestException(errMsg);
		}
		catch (ReaderException ex)
		{
			_logger.Error(ex.Message);
			// Invalid column separator selection
			return new ServiceResult(ResultCodeConst.File_Warning0003,
				await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0003));
		}
		catch (NotSupportedException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke while import authors");
		}
    }

    public async Task<IServiceResult> ExportAsync(ISpecification<Author> spec)
    {
	    try
		{
			// Try to parse specification to AuthorSpecification
			var authorSpec = spec as AuthorSpecification;
			// Check if specification is null
			if (authorSpec == null)
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}				
			
			// Get all with spec
			var entities = await _unitOfWork.Repository<Author, int>()
				.GetAllWithSpecAsync(authorSpec, tracked: false);
			if (entities.Any()) // Exist data
			{
				// Map entities to dtos 
				var authorDtos = _mapper.Map<List<AuthorDto>>(entities);
				// Process export data to file
				var fileBytes = CsvUtils.ExportToExcel(
					authorDtos.ToAuthorCsvRecords());

				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					fileBytes);
			}
			
			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process export author to excel");
		}
    }
    
    private async Task<Dictionary<int, List<string>>> DetectWrongDataAsync(
	    List<AuthorCsvRecord> records,
	    string[]? scanningFields,
	    SystemLanguage lang)
    {
	    // Check system lang
	    var isEng = lang == SystemLanguage.English;
			
	    // Initialize dictionary to hold errors
	    var errorMessages = new Dictionary<int, List<string>>();
	    // Initialize hashset to store unique author code
	    var authorCodeSet = new HashSet<string>();
	    // Default row index set to second row, as first row is header
	    var currDataRow = 2;

	    foreach (var record in records)
	    {
		    // Initialize error list for the current row
		    var rowErrors = new List<string>();
		    
		    // Check author code unique
		    if (!string.IsNullOrEmpty(record.AuthorCode))
		    {
			    // Try add author code to unique set
			    if (!authorCodeSet.Add(record.AuthorCode))  
                {
                    // Fail to add due to duplicates
                    rowErrors.Add(isEng 
	                    ? $"Author code {record.AuthorCode} is duplicated" 
	                    : $"Mã tác giả {record.AuthorCode} bị trùng");
                }
			    else // Add success -> Try to check duplicate in DB
			    {
				    var isExistAuthorCode = await _unitOfWork.Repository<Author, int>()
					    .AnyAsync(a => a.AuthorCode.ToLower() == record.AuthorCode.ToLower());
				    if (isExistAuthorCode) // Is duplicate in DB
				    {
					    // Add error
                        rowErrors.Add(isEng 
                            ? $"Author code {record.AuthorCode} has already exist" 
                            : $"Mã tác giả {record.AuthorCode} đã tồn tại");
				    }
			    }
		    }
		    else
		    {
			    rowErrors.Add(isEng 
				    ? "Author code is required" 
				    : "Vui lòng nhập mã tác giả ");
		    }
		    
		    // Check valid datetime
		    DateTime dob = DateTime.MinValue;
		    DateTime dateOfDeath = DateTime.MinValue;
		    if (!string.IsNullOrEmpty(record.Dob) // Invalid date of birth
		        && (!DateTime.TryParse(record.Dob, out dob) // Cannot parse
		            || dob < new DateTime(1900, 1, 1)     // Too old
		            || dob > DateTime.Now))                            // In the future
		    {
			    rowErrors.Add(isEng ? "Not valid date of birth" : "Ngày sinh không hợp lệ");
		    }else if (!string.IsNullOrEmpty(record.DateOfDeath) // Invalid hire date
		              && !DateTime.TryParse(record.DateOfDeath, out dateOfDeath))
		    {
			    rowErrors.Add(isEng ? "Not valid date of date" : "Ngày mất tác giả không hợp lệ");
		    }
		    if (dob != DateTime.MinValue && dateOfDeath != DateTime.MinValue)
		    {
			    if (dateOfDeath.Date < dob.Date)
			    {
				    rowErrors.Add(isEng ? "Not valid date of date" : "Ngày mất tác giả không hợp lệ");
			    }			    
		    }

		    // Check exist author information
		    var authorWithSameInfo = await _unitOfWork.Repository<Author, int>().GetWithSpecAsync(
			    new BaseSpecification<Author>(a =>
				    Equals(a.FullName.ToLower(), record.FullName.ToLower()) && // Same fullname
				    (
					    a.Nationality == null || (a.Nationality != null &&
					                              string.Equals(a.Nationality.ToLower(), record.Nationality.ToLower()))
				    ) && // Same nationality
				    (
					    !a.Dob.HasValue || 
					    (a.Dob.HasValue && dob != DateTime.MinValue && a.Dob.Value.Date == dob.Date)
				    ) && // Same Dob
				    (
					    !a.DateOfDeath.HasValue || 
					    (a.DateOfDeath.HasValue && dateOfDeath != DateTime.MinValue && a.DateOfDeath.Value.Date == dateOfDeath.Date)
				    ))); // Same Date of death 
		    if (authorWithSameInfo != null)
		    {
			    rowErrors.Add(isEng
				    ? "Added author name, dob, nationality already match with other author"
				    : "Tên tác giả, ngày sinh, quốc tịch đã tồn tại trong thông tin của tác giả khác");
		    }
		    
		    if (scanningFields != null)
		    {
			    // Initialize base spec
			    BaseSpecification<Author>? baseParam = null;
			    
			    // Iterate each fields to add criteria scanning logic
			    foreach (var field in scanningFields)
			    {
				    var normalizedField = field.ToUpperInvariant();
				    
				    // Building query to check duplicates on Author entity
				    var newSpec = normalizedField switch
				    {
					    var authorCode when authorCode == nameof(Author.AuthorCode).ToUpperInvariant() =>
						    new BaseSpecification<Author>(e => e.AuthorCode.Equals(record.AuthorCode)),
					    _ => null
				    };
				    
				    if (newSpec != null) // Found new author spec
				    {
					    // Combine specifications with AND logic
					    baseParam = baseParam == null
						    ? newSpec
						    : baseParam.Or(newSpec);
				    }
			    }
			    
			    // Check exist with spec
			    // if (baseParam != null && await _unitOfWork.Repository<Author, int>().AnyAsync(baseParam))
			    // {
				    // rowErrors.Add(isEng ? "Duplicate author code" : "Trùng mã tác giả");
			    // }
		    }
			
		    // if errors exist for the row, add to the dictionary
		    if (rowErrors.Any())
		    {
			    errorMessages.Add(currDataRow, rowErrors);
		    }

		    // Increment the row counter
		    currDataRow++;
	    }
	    
	    return errorMessages;
    }
    
    private Dictionary<int, List<int>> DetectDuplicates(List<AuthorCsvRecord> records, string[]? scanningFields)
    {
	    if (scanningFields == null || scanningFields.Length == 0)
		    return new Dictionary<int, List<int>>();

	    var duplicates = new Dictionary<int, List<int>>();
	    var keyToIndexMap = new Dictionary<string, int>();
	    var seenKeys = new HashSet<string>();

	    for (int i = 0; i < records.Count; i++)
	    {
		    var record = records[i];

		    // Generate a unique key based on scanning fields
		    var key = string.Join("|", scanningFields.Select(field => 
		    {
			    var normalizedField = field.ToUpperInvariant();
			    return normalizedField switch
			    {
				    var authorCode when authorCode == nameof(Author.AuthorCode).ToUpperInvariant() => record.AuthorCode?.Trim().ToUpperInvariant(),
				    _ => null
			    };
		    }).Where(value => !string.IsNullOrEmpty(value)));

		    if (string.IsNullOrEmpty(key))
			    continue;

		    // Check if the key is already seen
		    if (seenKeys.Contains(key))
		    {
			    // Find the first item of the duplicate key
			    var firstItemIndex = keyToIndexMap[key];

			    // Add the current index to the list of duplicates for this key
			    if (!duplicates.ContainsKey(firstItemIndex))
			    {
				    duplicates[firstItemIndex] = new List<int>();
			    }

			    duplicates[firstItemIndex].Add(i);
		    }
		    else
		    {
			    // Add the key
			    seenKeys.Add(key);
			    // map it to the current index
			    keyToIndexMap[key] = i;
		    }
	    }

	    return duplicates;
    }
}