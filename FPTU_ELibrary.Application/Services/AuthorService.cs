using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AuthorService : GenericService<Author, AuthorDto, int>, IAuthorService<AuthorDto>
{
    public AuthorService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
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
			var totalEmployeeWithSpec = await _unitOfWork.Repository<Author, int>().CountAsync(authorSpec);
			// Count total page
			var totalPage = (int)Math.Ceiling((double)totalEmployeeWithSpec / authorSpec.PageSize);
			
			// Set pagination to specification after count total employees 
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
					authorSpec.PageIndex, authorSpec.PageSize, totalPage);
				
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

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
		    // Build a base specification to filter by AuthorId
		    var baseSpec = new BaseSpecification<Author>(a => a.AuthorId == id);

		    // Retrieve author with specification
		    var authorEntity = await _unitOfWork.Repository<Author, int>().GetWithSpecAsync(baseSpec);
		    if (authorEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, nameof(Author).ToLower()));
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
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
			}
			
			// Process add author book related details
			// Build spec 
			var spec = new BaseSpecification<BookEditionAuthor>(ba => ba.AuthorId == id);
			// Enable split query
			spec.EnableSplitQuery();
		    // Include book edition and book reviews
		    spec.ApplyInclude(q => q
			    .Include(b => b.BookEdition)
					.ThenInclude(be => be.BookReviews));
		    // Order by RatingValue of BookReviews
		    spec.AddOrderBy(q => q
			    .BookEdition.BookReviews.Average(br => br.RatingValue));
		    
			// Get all author with specification
			var getAllAuthorEdition = await _unitOfWork.Repository<BookEditionAuthor, int>()
				.GetAllWithSpecAsync(spec);
			// Convert to list
			var authorBookEditions = getAllAuthorEdition.ToList();
			
			// Count author total published books
			var totalAuthorPublishedBook = authorBookEditions.Count;
			// Count avg book reviews
			var totalBookReviewAvg = authorBookEditions
				.SelectMany(x => x.BookEdition.BookReviews)
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
					TopReviewedBooks = authorBookEditions.Select(bAuthor => 
						new AuthorTopReviewedBookDto
						{
							BookEdition = _mapper.Map<BookEditionDto>(bAuthor.BookEdition)
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
    		
		    // // Check if author has constraints data
		    // var hasConstraints = await _unitOfWork.Repository<BookEditionAuthor, int>()
			   //  .AnyAsync(ba => ba.AuthorId == id);
		    // if (hasConstraints)
		    // {
			   //  // Cannot delete because it is bound to other data
			   //  return new ServiceResult(ResultCodeConst.SYS_Fail0007,
				  //   await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
		    // }
		    
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
    		throw new Exception("Error invoke when process soft delete employee");	
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
		    throw new Exception("Error invoke when process soft delete employee");	
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
}