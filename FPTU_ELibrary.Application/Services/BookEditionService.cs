using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Cloudinary;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Elastic.Models;
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
using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class BookEditionService : GenericService<BookEdition, BookEditionDto, int>, 
    IBookEditionService<BookEditionDto>
{
	// Configure lazy service
	private readonly Lazy<IBookService<BookDto>> _bookService;
	private readonly Lazy<IBookEditionAuthorService<BookEditionAuthorDto>> _editionAuthorService;
	private readonly Lazy<IBookEditionCopyService<BookEditionCopyDto>> _editionCopyService;
	private readonly Lazy<IElasticService> _elasticService;
	
	private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
	private readonly ICloudinaryService _cloudService;
	private readonly ICategoryService<CategoryDto> _cateService;
	private readonly IAuthorService<AuthorDto> _authorService;

	public BookEditionService(
		// Lazy service
		Lazy<IBookService<BookDto>> bookService,
		Lazy<IElasticService> elasticService,
		Lazy<IBookEditionAuthorService<BookEditionAuthorDto>> editionAuthorService,
		Lazy<IBookEditionCopyService<BookEditionCopyDto>> editionCopyService,
		// Normal service
		IAuthorService<AuthorDto> authorService,
		ICategoryService<CategoryDto> cateService,
		ICloudinaryService cloudService,
	    ILibraryShelfService<LibraryShelfDto> libShelfService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
    : base(msgService, unitOfWork, mapper, logger)
	{
		_authorService = authorService;
		_bookService = bookService;
		_cloudService = cloudService;
		_cateService = cateService;
	    _libShelfService = libShelfService;
	    _elasticService = elasticService;
	    _editionCopyService = editionCopyService;
	    _editionAuthorService = editionAuthorService;
	}
	
    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<BookEdition> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to BookEditionSpecification
            var bookEditionSpec = specification as BookEditionSpecification;
            // Check if specification is null
            if (bookEditionSpec == null)
            {
            	return new ServiceResult(ResultCodeConst.SYS_Fail0002,
            		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Count total book editions
            var totalBookEditionWithSpec = await _unitOfWork.Repository<BookEdition, int>().CountAsync(bookEditionSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalBookEditionWithSpec / bookEditionSpec.PageSize);
            
            // Set pagination to specification after count total book edition
            if (bookEditionSpec.PageIndex > totalPage 
                || bookEditionSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                bookEditionSpec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            bookEditionSpec.ApplyPaging(
                skip: bookEditionSpec.PageSize * (bookEditionSpec.PageIndex - 1), 
                take: bookEditionSpec.PageSize);
            
            // Get all with spec and selector
            var bookEditions = await _unitOfWork.Repository<BookEdition, int>()
                .GetAllWithSpecAndSelectorAsync(bookEditionSpec, be => new BookEdition()
                {
                    BookEditionId = be.BookEditionId,
                    BookId = be.BookId,
                    EditionTitle = be.EditionTitle,
                    EditionSummary = be.EditionSummary,
                    EditionNumber = be.EditionNumber,
                    PageCount = be.PageCount,
                    Language = be.Language,
                    PublicationYear = be.PublicationYear,
                    CoverImage = be.CoverImage,
                    Format = be.Format,
                    Publisher = be.Publisher,
                    Isbn = be.Isbn,
                    IsDeleted = be.IsDeleted,
                    CanBorrow = be.CanBorrow,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    ShelfId = be.ShelfId,
                    Shelf = be.Shelf,
                    Status = be.Status,
                    IsTrained = be.IsTrained,
                    TrainedDay = be.TrainedDay,
                    EstimatedPrice = be.EstimatedPrice,
                    BookEditionInventory = be.BookEditionInventory,
                    BookEditionCopies = be.BookEditionCopies,
                    BookReviews = be.BookReviews,
                    BookEditionAuthors = be.BookEditionAuthors.Select(ba => new BookEditionAuthor()
                    {
	                    BookEditionAuthorId = ba.BookEditionAuthorId,
	                    BookEditionId = ba.BookEditionId,
	                    AuthorId = ba.AuthorId,
	                    Author = ba.Author
                    }).ToList(),
                    Book = new Book()
                    {
                        BookId = be.Book.BookId,
                        BookCode = be.Book.BookCode,
                        Title = be.Book.Title,
                        SubTitle = be.Book.SubTitle,
                        Summary = be.Book.Summary,
                        IsDeleted = be.Book.IsDeleted,
                        CreatedAt = be.Book.CreatedAt,
                        CreatedBy = be.Book.CreatedBy,
                        UpdatedAt = be.Book.UpdatedAt,
                        UpdatedBy = be.Book.UpdatedBy,
                        BookResources = be.Book.BookResources,
                        BookCategories = be.Book.BookCategories.Select(bc => new BookCategory()
                        {
                            BookCategoryId = bc.BookCategoryId,
                            BookId = bc.BookId,
                            CategoryId = bc.CategoryId,
                            Category = bc.Category
                        }).ToList()
                    }
                });

            if (bookEditions.Any()) // Exist data
            {
                // Convert to dto collection
                var bookEditionDtos = _mapper.Map<List<BookEditionDto>>(bookEditions);
                
                // Handle enum text
                // bookEditionDtos = HandleEnumTextForEditions(bookEditionDtos);
                
                // Convert to book edition table rows
                var tableRows = bookEditionDtos.ToEditionTableRows();
             
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<BookEditionTableRowDto>(tableRows,
                	bookEditionSpec.PageIndex, bookEditionSpec.PageSize, totalPage, totalBookEditionWithSpec);
                
                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<BookEditionDto>>(bookEditions));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke process when get all book");
        }
    }
	
    public override async Task<IServiceResult> UpdateAsync(int id, BookEditionDto dto)
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
				
				// Ignores authors, edition copies
				if (errors.ContainsKey("bookEditionAuthors")) errors.Remove("bookEditionAuthors");
				else if(errors.ContainsKey("bookEditionCopies")) errors.Remove("bookEditionCopies");

				if (errors.Any())
				{
					throw new UnprocessableEntityException("Invalid validations", errors);
				}
			}

			// Check exist shelf location
			if (dto.ShelfId != null 
			    && int.TryParse(dto.ShelfId.ToString(), out var validShelfId) && validShelfId > 0) // ShelfId must be numeric
			{
				var checkExistShelfRes = await _libShelfService.AnyAsync(x => x.ShelfId == validShelfId);
				if (checkExistShelfRes.Data is false)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "shelf location to process update" : "vị trí kệ sách để sửa"));
				}
			}		
			
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetByIdAsync(id);
			if (existingEntity == null || existingEntity.IsDeleted)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "book edition to process update" : "ấn bản để sửa"));
			}
			
			// Require transitioning to Draft status to modify or soft-delete a book
			if (existingEntity.Status != BookEditionStatus.Draft)
			{
				return new ServiceResult(ResultCodeConst.Book_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0002));
			}
			
			// Initialize custom errors
			var customErrs = new Dictionary<string, string[]>();
			
			// Check duplicate edition number (if change)
			if (!Equals(existingEntity.EditionNumber, dto.EditionNumber))
			{
				var isEditionNumDuplicate = await _unitOfWork.Repository<BookEdition, int>()
					.AnyAsync(x => x.EditionNumber == dto.EditionNumber && // Any other edition number match 
					               x.BookId == existingEntity.BookId); // from book that possessed the edition
				if (isEditionNumDuplicate)
				{
					customErrs.Add("editionNumber", [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
				}
			}
			
			// Check exist isbn (if change)
			if (!Equals(existingEntity.Isbn, dto.Isbn))
			{
				var isIsbnExist = await _unitOfWork.Repository<BookEdition, int>()
					.AnyAsync(be => be.Isbn == dto.Isbn && // Any ISBN found 
					                be.BookEditionId != id); // Except request book edition
				if (isIsbnExist)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0007);
					customErrs.Add("isbn", [StringUtils.Format(errMsg, $"'{dto.Isbn}'")]);
				}
			}
			
			// Check exist cover image
			if (!Equals(existingEntity.CoverImage, dto.CoverImage) // Detect as cover image change 
			    && !string.IsNullOrEmpty(dto.CoverImage)) 
			{
				// Initialize field
				var isImageExist = true;
				
				// Extract public id from update entity
				var updatePublicId = StringUtils.GetPublicIdFromUrl(dto.CoverImage);
				if (string.IsNullOrEmpty(updatePublicId)) // Provider public id must be existed
				{
					isImageExist = false;
				}
				else // Exist public id
				{
					// Check existence on cloud
					var isImageOnCloud = (await _cloudService.IsExistAsync(updatePublicId, FileType.Image)).Data is true;
					if (!isImageOnCloud)
					{
						isImageExist = false;
					}
				}
				
				// Check if existing entity already has image
				if (!string.IsNullOrEmpty(existingEntity.CoverImage))
				{
					// Extract public id from current entity
					var currentPublicId	= StringUtils.GetPublicIdFromUrl(existingEntity.CoverImage);
					if (!Equals(currentPublicId, updatePublicId)) // Error invoke when update provider update id 
					{
						// Mark as fail to update
						return new ServiceResult(ResultCodeConst.SYS_Fail0003,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
					}
				}

				if (!isImageExist) // Invoke error image not found
				{
					// Return as not found image resource
					return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
						await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
				}
			}
			
			// Check if any errors invoke
			if (customErrs.Any())
			{
				throw new UnprocessableEntityException("Invalid data", customErrs);
			}
			
			// Process update entity
			existingEntity.EditionTitle = dto.EditionTitle;
			existingEntity.EditionSummary = dto.EditionSummary;
			existingEntity.EditionNumber = dto.EditionNumber;
			existingEntity.PublicationYear = dto.PublicationYear;
			existingEntity.Publisher = dto.Publisher;
			existingEntity.PageCount = dto.PageCount;
			existingEntity.Language = dto.Language;
			existingEntity.CoverImage = dto.CoverImage;
			existingEntity.Format = dto.Format;
			existingEntity.Isbn = dto.Isbn;
			existingEntity.EstimatedPrice = dto.EstimatedPrice;
			
			// Only process update shelf when valid
			if (dto.ShelfId > 0)
			{
				existingEntity.ShelfId = dto.ShelfId;
			}
			
			// Progress update when all require passed
			await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(existingEntity);

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
			throw;
		}
    }
		
    public override async Task<IServiceResult> DeleteAsync(int id)
    {
	    try
	    {
		    // Determine current lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
			    
		    // Build a base specification to filter by BookEditionId
		    var baseSpec = new BaseSpecification<BookEdition>(a => a.BookEditionId == id);
			// Apply including authors
			baseSpec.ApplyInclude(q => q
				.Include(be => be.BookEditionAuthors));
		    
		    // Retrieve book edition with specification
		    var bookEditionEntity = await _unitOfWork.Repository<BookEdition, int>().GetWithSpecAsync(baseSpec);
		    if (bookEditionEntity == null)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book edition to process delete" : "ấn bản để xóa"));
		    }

		    // Check whether book edition in the trash bin or not in draft status
		    if (!bookEditionEntity.IsDeleted)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }

		    // Try to delete all book edition authors (if any)
		    if (bookEditionEntity.BookEditionAuthors.Any())
		    {
			    // Process delete range without save changes
			    await _editionAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
				    // Select all existing bookEditionAuthorId
				    bookEditionEntity.BookEditionAuthors.Select(ba => ba.BookEditionAuthorId).ToArray());
		    }
		    
		    // Perform delete book edition, and delete cascade with BookEditionInventory
		    await _unitOfWork.Repository<BookEdition, int>().DeleteAsync(id);
		    
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
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when progress delete data");
	    }
    }
	
    public async Task<IServiceResult> CreateAsync(int bookId, BookEditionDto dto)
    {
	    try
	    {
		    // Determine current lang 
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
		    
		    // Check book existence
		    var isBookExist = (await _bookService.Value.AnyAsync(x => x.BookId == bookId)).Data is true;
		    if (!isBookExist)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "sách để thêm ấn bản" : "book to add edition"));
		    }
		    
		    // Select list of author ids
		    var authorIds = dto.BookEditionAuthors
			    .Select(be => be.AuthorId)
			    .Distinct() // Eliminate same authorId from may book edition
			    .ToList();
		    // Count total exist result
		    var countAuthorResult = await _authorService.CountAsync(
			    new BaseSpecification<Author>(ct => authorIds.Contains(ct.AuthorId)));
		    // Check exist any author not being counted
		    if (int.TryParse(countAuthorResult.Data?.ToString(), out var totalAuthor) // Parse result to integer
		        && totalAuthor != authorIds.Count) // Not exist 1-many author
		    {
			    return new ServiceResult(ResultCodeConst.Book_Warning0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0002));
		    }
		    
		    // Custom error responses
		    var customErrors = new Dictionary<string, string[]>();

		    // Check unique book edition number
		    var isEditionNumberExist = await _unitOfWork.Repository<BookEdition, int>()
			    .AnyAsync(x => x.BookId == bookId && x.EditionNumber == dto.EditionNumber);
		    if (isEditionNumberExist)
		    {
			    // Add error 
			    customErrors.Add("editionNumber", [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
		    }
		    
		    // Check exist cover image
            if (!string.IsNullOrEmpty(dto.CoverImage))
            {
            	// Initialize field
            	var isImageOnCloud = true;
            	
            	// Extract provider public id
            	var publicId = StringUtils.GetPublicIdFromUrl(dto.CoverImage);
            	if (publicId != null) // Found
            	{
            		// Process check exist on cloud			
            		isImageOnCloud = (await _cloudService.IsExistAsync(publicId, FileType.Image)).Data is true;
            	}

            	if (!isImageOnCloud || publicId == null) // Not found image or public id
            	{
            		// Mark as not found image
            		return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
            			await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
            	}
            }

            // Iterate each book edition copy (if any) to check valid data
            var listBookEditionCopy = dto.BookEditionCopies.ToList();
            for (int i = 0; i < listBookEditionCopy.Count; ++i)
            {
	            var bec = listBookEditionCopy[i];
	            // Check exist edition copy barcode within DB
	            var isCodeExist = await _unitOfWork.Repository<BookEditionCopy, int>()
		            .AnyAsync(x => x.Barcode == bec.Barcode);
	            if (isCodeExist)
	            {
		            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0006);
		            // Add errors
		            customErrors.Add($"bookCopies[{i}].barcode", [StringUtils.Format(errMsg, $"'{bec.Barcode}'")]);
	            }
                    
	            // Default status
	            bec.Status = nameof(BookEditionCopyStatus.OutOfShelf);
	            // Boolean 
	            bec.IsDeleted = false;
            }
		    
            // Default value
            // Assign book id
            dto.BookId = bookId;
            dto.IsDeleted = false;
            dto.CanBorrow = false;
            // Clear ISBN hyphens
            dto.Isbn = ISBN.CleanIsbn(dto.Isbn);
            // Check exist Isbn
            var isIsbnExist = await _unitOfWork.Repository<BookEdition, int>()
	            .AnyAsync(x => x.Isbn == dto.Isbn);
            if (isIsbnExist) // already exist 
            {
	            // Add error
	            customErrors.Add(
		            "isbn", 
		            // Isbn already exist message
		            [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0007)]);
            }
			
			// Any errors invoke when checking valid data
			if (customErrors.Any()) // exist errors
			{
				throw new UnprocessableEntityException("Invalid data", customErrors);
			}
			
			// Process create new book
			await _unitOfWork.Repository<BookEdition, int>().AddAsync(_mapper.Map<BookEdition>(dto));
			// Save to DB
			if (await _unitOfWork.SaveChangesAsync() > 0) // Save successfully
			{
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
		    throw new Exception("Error invoke when process create new book edition");
	    }
    }
    
    public async Task<IServiceResult> GetDetailAsync(int id)
    {
        try
        {
            // Build specification
			var baseSpec = new BaseSpecification<BookEdition>(b => b.BookEditionId == id);
			var bookEditionEntity = await _unitOfWork.Repository<BookEdition, int>()
				.GetWithSpecAndSelectorAsync(baseSpec, be => new BookEdition()
				{
					BookEditionId = be.BookEditionId,
                    BookId = be.BookId,
                    EditionTitle = be.EditionTitle,
                    EditionSummary = be.EditionSummary,
                    EditionNumber = be.EditionNumber,
                    PageCount = be.PageCount,
                    Language = be.Language,
                    PublicationYear = be.PublicationYear,
                    CoverImage = be.CoverImage,
                    Format = be.Format,
                    Publisher = be.Publisher,
                    Isbn = be.Isbn,
                    IsDeleted = be.IsDeleted,
                    CanBorrow = be.CanBorrow,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    CreatedBy = be.CreatedBy,
                    UpdatedBy = be.UpdatedBy,
                    ShelfId = be.ShelfId,
                    Shelf = be.Shelf,
                    Status = be.Status,
                    IsTrained = be.IsTrained,
                    TrainedDay = be.TrainedDay,
                    BookEditionInventory = be.BookEditionInventory,
                    BookEditionCopies = be.BookEditionCopies,
                    BookReviews = be.BookReviews,
                    BookEditionAuthors = be.BookEditionAuthors.Select(ba => new BookEditionAuthor()
                    {
	                    BookEditionAuthorId = ba.BookEditionAuthorId,
	                    BookEditionId = ba.BookEditionId,
	                    AuthorId = ba.AuthorId,
	                    Author = ba.Author
                    }).ToList(),
                    Book = new Book()
                    {
                        BookId = be.Book.BookId,
                        BookCode = be.Book.BookCode,
                        Title = be.Book.Title,
                        SubTitle = be.Book.SubTitle,
                        Summary = be.Book.Summary,
                        IsDeleted = be.Book.IsDeleted,
                        CreatedAt = be.Book.CreatedAt,
                        CreatedBy = be.Book.CreatedBy,
                        UpdatedAt = be.Book.UpdatedAt,
                        UpdatedBy = be.Book.UpdatedBy,
                        BookResources = be.Book.BookResources,
                        BookCategories = be.Book.BookCategories.Select(bc => new BookCategory()
                        {
                            BookCategoryId = bc.BookCategoryId,
                            BookId = bc.BookId,
                            CategoryId = bc.CategoryId,
                            Category = bc.Category
                        }).ToList()
                    }
				});

			if (bookEditionEntity != null)
            {
                // Map to dto
                var dto = _mapper.Map<BookEditionDto>(bookEditionEntity);
                // Handle enum text
                // dto = HandleEnumTextForSingleEdition(dto);
                // Convert to book edition detail dto
                var bookEditionDetailDto = dto.ToEditionDetailDto();
                
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), bookEditionDetailDto);
			}

			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get book edition by id");
        }
    }

    public async Task<IServiceResult> UpdateStatusAsync(int id)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == id);
			// Apply include
			baseSpec.ApplyInclude(q => q
				// Include edition inventory
				.Include(x => x.BookEditionInventory)
				// Include book
				.Include(x => x.Book)
					// Include book categories
					.ThenInclude(x => x.BookCategories)
						// Include category
						.ThenInclude(x => x.Category)
				// Include book edition authors
				.Include(x => x.BookEditionAuthors)
					// Include author
					.ThenInclude(bea => bea.Author)
				// Include book edition copies
				.Include(x => x.BookEditionCopies)
			);
			
		    // Retrieve book edition with specific ID 
			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null || existingEntity.IsDeleted) // Check whether book exist or marking as deleted
			{
				var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMSg, isEng ? "book edition" : "ấn bản"));
			}
			
			// Check current book edition status
			if (existingEntity.Status == BookEditionStatus.Draft) // Draft -> Published
			{
				// Initialize err msg
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0012);
				// Validate edition information before published
				// Check for shelf location
				if (existingEntity.ShelfId == null || existingEntity.ShelfId == 0)
				{
					return new ServiceResult(ResultCodeConst.Book_Warning0012,
						StringUtils.Format(errMsg, isEng 
							? "Shelf location not found" : "Không tìm thấy vị trí kệ cho sách"));
				}
				if (existingEntity.ShelfId > 0)
				{
					// Check for exist at least one book edition copy that mark as in shelf
					if (existingEntity.BookEditionCopies.All(x => x.Status != nameof(BookEditionCopyStatus.InShelf)))
					{
						return new ServiceResult(ResultCodeConst.Book_Warning0012,
							StringUtils.Format(errMsg, isEng 
								? "Required at least one copy in shelf" 
								: "Cần ít nhất một bản in có sẵn trên kệ"));
					}	
				}
				
				// Process change status
				existingEntity.Status = BookEditionStatus.Published;

				// Process update change to DB
				if (await _unitOfWork.SaveChangesAsync() > 0) // Success
				{
					// Initialize bool field
					var isAddToElastic = false;
					// Synchronize data to ElasticSearch
                    if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.BookIndex))
                    {
	                    // Convert to BookEditionDto
	                    var dto = _mapper.Map<BookEditionDto>(existingEntity);
	                    
	                    // Check whether exist any document
	                    var isExistAnyDocument = await _elasticService.Value.DocumentExistsAsync<ElasticBook>(
		                    documentId: existingEntity.BookId.ToString());

	                    if (!isExistAnyDocument) // Not found
	                    {
		                    // Process add both root and nested object
		                    isAddToElastic = await _elasticService.Value.AddOrUpdateAsync(
			                    document: dto.ToElasticBook(),
			                    documentKeyName: nameof(ElasticBook.BookId)); // Custom elastic _id with BookId value
	                    }
	                    else // Add only nested object
	                    {
		                    // Get nested object fields name 
		                    var nestedFieldName = ElasticUtils.GetElasticFieldName<ElasticBook>(nameof(ElasticBook.BookEditions)) ?? string.Empty;
		                    // Get nested object key name
		                    var nestedKey = ElasticUtils.GetElasticFieldName<ElasticBookEdition>(nameof(ElasticBookEdition.BookEditionId)) ?? string.Empty;
		                    
                    		// Add new book to elastic
                    		isAddToElastic = await _elasticService.Value.AddOrUpdateNestedAsync<ElasticBook, ElasticBookEdition>(
			                    documentId: existingEntity.BookId.ToString(),
			                    nestedFieldName: nestedFieldName,
								nestedObject: dto.ToElasticBookEdition(),
			                    nestedKey: nestedKey,
			                    nestedKeyValue: existingEntity.BookEditionId.ToString());
	                    }
                    }
                    
                    // Custom message for failing to synchronize data to elastic
                    var msg = !isAddToElastic 
	                    ? isEng ? ", but fail to add data to Elastic" : ", nhưng cập nhật dữ liệu mới vào Elastic thất bại"
	                    : string.Empty;
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
	                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
				}
			}
			else if (existingEntity.Status == BookEditionStatus.Published) // Published -> Draft
			{
				// Initialize err msg
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0013);
				// Check whether book in borrow status (One or more copy now in store in library shelf) 
				if (existingEntity.CanBorrow) 
				{
					// Do not allow to change status
					return new ServiceResult(ResultCodeConst.Book_Warning0013,
						StringUtils.Format(errMsg, isEng ? "There still exist copy in shelf" : "Vẫn còn sách ở trên kệ"));
				}
				
				// Check inventory total whether to allow change status to Draft
				if (existingEntity.BookEditionInventory != null &&
				    (existingEntity.BookEditionInventory.RequestCopies > 0 ||
				     existingEntity.BookEditionInventory.BorrowedCopies > 0 ||
				     existingEntity.BookEditionInventory.ReservedCopies > 0))
				{
					// Cannot change data that is on borrowing or reserved
					return new ServiceResult(ResultCodeConst.Book_Warning0013,
                    	StringUtils.Format(errMsg, isEng 
						? "Copy is on borrowing or reserved" 
						: "Bản in đang được mượn hoặc được đặt trước"));
				}
				
				// Process change status
				existingEntity.Status = BookEditionStatus.Draft;

				// Process update change to DB
				if (await _unitOfWork.SaveChangesAsync() > 0) // Success
				{
					// Initialize bool field
					var isDeletedNested = false;
					// Progress delete data in Elastic
					if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.BookIndex))
					{
						// Get nested object fields name 
						var nestedFieldName = ElasticUtils.GetElasticFieldName<ElasticBook>(nameof(ElasticBook.BookEditions)) ?? string.Empty;
						// Get nested object key name
						var nestedKey = ElasticUtils.GetElasticFieldName<ElasticBookEdition>(nameof(ElasticBookEdition.BookEditionId)) ?? string.Empty;
						
						// Check whether the nested object exist
						var isNestedObjectExist = await _elasticService.Value.NestedExistAsync<ElasticBookEdition>(
							documentId: existingEntity.BookId.ToString(),
							nestedFieldName: nestedFieldName,
							nestedKey: nestedKey,
							nestedKeyValue: existingEntity.BookEditionId.ToString());

						if (isNestedObjectExist) // Is Exist
						{
							// Process delete nested object
							isDeletedNested = await _elasticService.Value.DeleteNestedAsync<ElasticBook>(
								documentId: existingEntity.BookId.ToString(),
								nestedFieldName: nestedFieldName,
								nestedKey: nestedKey,
								nestedKeyValue: existingEntity.BookEditionId.ToString());
						}
					}
					
					// Custom message for failing to synchronize data to elastic
					var msg = !isDeletedNested 
						? isEng ? ", but fail to delete Elastic data" : ", nhưng xóa dữ liệu Elastic thất bại"
						: string.Empty;
					return new ServiceResult(ResultCodeConst.SYS_Success0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
				}
			}
			
			// Mark as fail to save
			return new ServiceResult(ResultCodeConst.SYS_Fail0003,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update book edition status");
	    }
    }
    
    public async Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow)
    {
	    try
	    {
		    // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
			// Retrieve book edition by id
			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetByIdAsync(id);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng 
					? "book edition to update borrow status" 
					: "ấn bản để sửa trạng thái có thể mượn"), false);
			}
			
			// Update status
			existingEntity.CanBorrow = canBorrow;
			
			// Progress update without change 
			await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(existingEntity);
			
			// Mark as update success
			return new ServiceResult(ResultCodeConst.SYS_Success0003,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update edition borrow status");
	    }
    }

    public async Task<IServiceResult> UpdateShelfLocationAsync(int id, int? shelfId)
    {
	    try
	    {	
		    // Determine current lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == id);
		    // Apply include 
		    baseSpec.ApplyInclude(q => q
			    .Include(be => be.BookEditionCopies)
		    );
		    
			// Retrieve book edition by id
			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "book edition" : "ấn bản"), false);
			}
			
			// Check exist shelf location
			var existingShelf = (await _libShelfService.AnyAsync(lf => lf.ShelfId == shelfId)).Data is true;
			if (!existingShelf && shelfId != null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "shelf location" : "kệ sách"));
			}
			else
			{
				// Check whether book already assigned to current shelf location
				if (existingEntity.ShelfId == shelfId) // same shelf location
				{
					// Mark as update success
					return new ServiceResult(ResultCodeConst.SYS_Success0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
				}
			}
			
			// Required all book copy must be in out-of-shelf status
            if (existingEntity.BookEditionCopies
                .Select(bec => bec.Status)
                .Any(status => status != nameof(BookEditionCopyStatus.OutOfShelf)))
            {
            	// Msg: Cannot process, please move all edition copy status to inventory first
            	return new ServiceResult(ResultCodeConst.Book_Warning0009,
            		await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0009));
            }
			
			// Process update shelf location
			existingEntity.ShelfId = shelfId;

			if (await _unitOfWork.SaveChangesAsync() > 0)
			{
				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
			}
			
			// Mark as update fail
			return new ServiceResult(ResultCodeConst.SYS_Fail0003,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update shelf location for book edition");
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

		    // Build spec
		    var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == id);
		    // Include all constraints to update soft delete
		    baseSpec.ApplyInclude(q => q
			    // Ignore include all authors, due to book edition can exist without any book belongs to    
			    // Include all book edition copy
			    .Include(be => be.BookEditionCopies)
		    );
		    // Get book edition with spec
		    var existingEntity = await _unitOfWork.Repository<BookEdition, int>()
			    .GetWithSpecAsync(baseSpec);
		    // Check if book edition already mark as deleted
		    if (existingEntity == null || existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book edition" : "ấn bản"));
		    }

		    // Require transitioning to Draft status to modify or soft-delete a book
		    if (existingEntity.Status != BookEditionStatus.Draft)
		    {
			    return new ServiceResult(ResultCodeConst.Book_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0002));
		    }

		    // Check whether book edition contains any copy, which mark as not deleted yet
		    if (existingEntity.BookEditionCopies.Any(x => !x.IsDeleted))
		    {
			    // Extract all current edition copy ids
			    var editionCopyIds = existingEntity.BookEditionCopies
				    .Where(bec => !bec.IsDeleted)
				    .Select(bec => bec.BookEditionCopyId).ToList();
			    if (editionCopyIds.Any()) // Found any copy is not deleted yet
			    {
				    // Try to soft delete all related edition copies
				    var deleteResult = await _editionCopyService.Value.SoftDeleteRangeAsync(
					    bookEditionId: existingEntity.BookEditionId,
					    bookEditionCopyIds: editionCopyIds);
				    // Check whether fail to delete range book edition copies
				    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
			    }
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

		    return new ServiceResult(ResultCodeConst.SYS_Success0007,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
	    }
	    catch (UnprocessableEntityException)
	    {
		    return new ServiceResult(ResultCodeConst.Book_Warning0008,
			    await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008));
	    }
        catch (Exception ex)
        {
        	_logger.Error(ex.Message);	
        	throw new Exception("Error invoke when process soft delete book edition");	
        }
    }
	
    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
	    try
        {
	        // Build spec
	        var baseSpec = new BaseSpecification<BookEdition>(x => ids.Contains(x.BookEditionId));
	        // Include all constraints to update soft delete
	        baseSpec.ApplyInclude(q => q
		        // Ignore include all authors, due to book edition can exist without any book belongs to    
		        // Include all book edition copy
		        .Include(be => be.BookEditionCopies)
	        );
            var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var bookEditionList = bookEditionEntities.ToList();
            if (bookEditionList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }else if (bookEditionList.Any(x => x.Status != BookEditionStatus.Draft))
            {
	            // Require transitioning to Draft status to modify or soft-delete a book
	            return new ServiceResult(ResultCodeConst.Book_Fail0002,
		            await _msgService.GetMessageAsync(ResultCodeConst.Book_Fail0002));
            }
            
            // Check whether book edition contains any copy, which mark as not deleted yet
            if (bookEditionList.SelectMany(be => be.BookEditionCopies).Any(bec => !bec.IsDeleted)) 
            {
	            // Iterate each edition to soft delete all copies
	            foreach (var edition in bookEditionList)
	            {
		            // Extract all current edition copy ids
		            var editionCopyIds = edition.BookEditionCopies
			            .Where(bec => !bec.IsDeleted)
			            .Select(bec => bec.BookEditionCopyId).ToList();
		            if (editionCopyIds.Any())
		            {
			            // Try to soft delete all related edition copies
			            var deleteResult = await _editionCopyService.Value.SoftDeleteRangeAsync(
				            bookEditionId: edition.BookEditionId, 
				            bookEditionCopyIds: editionCopyIds);
			            // Check whether fail to delete range book edition copies
			            if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
		            }
	            }
            }

            // Iterate each book edition
            foreach (var be in bookEditionList)
            {
	            // Update deleted status
	            be.IsDeleted = true;
            }
            
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0007,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when remove range book edition");
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
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<BookEdition>(x => x.BookEditionId == id);
		    // Include all constraints to update soft delete
		    baseSpec.ApplyInclude(q => q
			    // Ignore include all authors, due to book edition can exist without any book belongs to    
			    // Include all book edition copy
			    .Include(be => be.BookEditionCopies)
		    );
		    var existingEntity = await _unitOfWork.Repository<BookEdition, int>()
			    .GetWithSpecAsync(baseSpec);
		    // Check if book edition already mark as deleted
		    if (existingEntity == null || !existingEntity.IsDeleted)
		    {
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "book edition" : "ấn bản"));
		    }
				
		    // Check whether book edition contains any copy, which mark as deleted
		    if (existingEntity.BookEditionCopies.Any(x => x.IsDeleted))
		    {
			    // Extract all current edition copy ids
			    var editionCopyIds = existingEntity.BookEditionCopies
				    .Where(bec => bec.IsDeleted)
				    .Select(bec => bec.BookEditionCopyId).ToList();
			    if (editionCopyIds.Any())
			    {
				    // Try to undo undo all related edition copies
				    var undoResult = await _editionCopyService.Value.UndoDeleteRangeAsync(
					    bookEditionId: existingEntity.BookEditionId, 
					    bookEditionCopyIds: editionCopyIds);
				    // Check whether fail to undo range book edition copies
				    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
			    }
		    }
		    
		    // Update delete status
		    existingEntity.IsDeleted = false;
		    
		    // Save changes to DB
		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
		    if (rowsAffected == 0)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
		    }

		    // Mark as update success
		    return new ServiceResult(ResultCodeConst.SYS_Success0009,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);	
		    throw new Exception("Error invoke when process undo delete book edition");	
	    }
    }
    
    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
    	try
        {
	        // Build spec
	        var baseSpec = new BaseSpecification<BookEdition>(x => ids.Contains(x.BookEditionId));
	        // Include all constraints to update soft delete
	        baseSpec.ApplyInclude(q => q
		        // Ignore include all authors, due to book edition can exist without any book belongs to    
		        // Include all book edition copy
		        .Include(be => be.BookEditionCopies)
	        );
	        var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
		        .GetAllWithSpecAsync(baseSpec);
	        // Check if any data already soft delete
	        var bookEditionList = bookEditionEntities.ToList();
            if (bookEditionList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            
            // Check whether book edition contains any copy, which mark as deleted
            if (bookEditionList.SelectMany(be => be.BookEditionCopies).Any(be => be.IsDeleted))
            {
	            // Iterate each edition to undo delete all copies
	            foreach (var edition in bookEditionList)
	            {
		            // Extract all current edition copy ids
		            var editionCopyIds = edition.BookEditionCopies
			            .Where(bec => bec.IsDeleted)
						.Select(bec => bec.BookEditionCopyId).ToList();
		            if (editionCopyIds.Any())
		            {
			            // Try to soft undo all related edition copies
			            var undoResult = await _editionCopyService.Value.UndoDeleteRangeAsync(
				            bookEditionId: edition.BookEditionId, 
				            bookEditionCopyIds: editionCopyIds);
			            // Check whether fail to undo range book edition copies
			            if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
		            }
	            }
            }

            // Iterate each book edition
            foreach (var be in bookEditionList)
            {
	            // Update deleted status
	            be.IsDeleted = false;
            }
                    
            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), true);
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
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
		    // Get all matching book edition 
		    // Build spec
		    var baseSpec = new BaseSpecification<BookEdition>(e => ids.Contains(e.BookEditionId));
		    // Apply including authors
		    baseSpec.ApplyInclude(q => q
			    .Include(be => be.BookEditionAuthors));
		    // Get all author with specification
		    var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
			    .GetAllWithSpecAsync(baseSpec);
		    // Check if any data already soft delete
		    var bookEditionList = bookEditionEntities.ToList();
		    if (bookEditionList.Any(x => !x.IsDeleted))
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		    }
		    
		    // Try to clear all authors existing in each of book edition (if any)
		    foreach (var be in bookEditionList)
		    {
			    // Process delete range without save changes
			    await _editionAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
				    // Select all existing bookEditionAuthorId
				    be.BookEditionAuthors.Select(ba => ba.BookEditionAuthorId).ToArray());
		    }

		    // Process delete range, and delete cascade with BookEditionInventory
		    await _unitOfWork.Repository<BookEdition, int>().DeleteRangeAsync(ids);
		    
		    // Save to DB
		    if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
		    {
			    var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
			    return new ServiceResult(ResultCodeConst.SYS_Success0008,
				    StringUtils.Format(msg, bookEditionList.Count.ToString()), true);
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
		    throw new Exception("Error invoke when process delete range book edition");
	    }
    }

    public async Task<IServiceResult> ImportAsync(
	    IFormFile? file, 
	    List<IFormFile> coverImageFiles,
	    string[]? scanningFields)
    {
	    try
	    {
		    // Determine system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
			    .CurrentLanguage);

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
			    throw new NotSupportedException(await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
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
			    CsvUtils.ReadCsvOrExcelWithErrors<BookEditionCsvRecord>(file, csvConfig, null, lang);
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
			
		    // Detect record errors
		    var detectResult =
			    await DetectWrongDataAsync(readResp.Records, imageFileNames, scanningFields, (SystemLanguage)lang!);
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

		    // Handle upload images (Image name | URL)
		    var uploadFailList = new List<string>();
		    var imageUrlDic = new Dictionary<string, string>();
		    foreach (var coverImage in coverImageFiles)
		    {
			    // Try to validate file
			    var validateResult = await 
				    new ImageTypeValidator(lang.ToString() ?? SystemLanguage.English.ToString()).ValidateAsync(coverImage);
			    if (!validateResult.IsValid)
			    {
				    var isEng = lang == SystemLanguage.English;
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
				    // Add image that fail to upload
				    uploadFailList.Add(coverImage.FileName);
			    }
			    else
			    {
				    // Add to dic
				    imageUrlDic.Add(coverImage.FileName, uploadResult.SecureUrl);
			    }
		    }

		    var totalImported = 0;
		    var totalFailed = 0;
		    // Process import book editions
		    var successRecords = readResp.Records
			    .Where(r => !uploadFailList.Contains(r.CoverImage))
			    .ToList();
		    var failRecords = new List<BookEditionCsvRecord>();
		    if (successRecords.Any())
		    {
			    // Group all editions have the same book code
			    var groupedRecords = successRecords
				    .GroupBy(r => r.BookCode)
				    .Select(e => new
				    {
					    Key = e.Key,
						Values = e.ToList()
				    });
			    
			    foreach (var record in groupedRecords)
			    {
				    // Initialize list editions
				    var editionList = new List<BookEditionDto>();
				    foreach (var bookEdition in record.Values)
				    {
					    // Extract all edition copy barcodes
					    var editionCopyBarcodes = !string.IsNullOrWhiteSpace(bookEdition.EditionCopyBarcodes)
						    ? bookEdition.EditionCopyBarcodes.Split("\n").Select(barcode => new BookEditionCopyDto()
						    {
							    Barcode = barcode,
							    IsDeleted = false,
							    Status = nameof(BookEditionCopyStatus.OutOfShelf)
						    }).ToList()
						    : new List<BookEditionCopyDto>();
					    
					    // Extract all edition authors
					    var editionAuthorCodes = bookEdition.AuthorCodes.Split("\n").ToList();
					    // Get all author by code
					    var authorDtos = (await _authorService.GetAllByCodesAsync(editionAuthorCodes.ToArray())).Data as List<AuthorDto>;
						
					    // Get shelf location
					    var shelfDto = 
						    (await _libShelfService.GetWithSpecAsync(new BaseSpecification<LibraryShelf>(
						    s => s.ShelfNumber.ToLower() == bookEdition.ShelfNumber.ToLower()))
						    ).Data as LibraryShelfDto;
					    
					    // Add new edition
					    editionList.Add(new BookEditionDto()
					    {
						    // Cover image
						    CoverImage = imageUrlDic.TryGetValue(bookEdition.CoverImage, out var coverImageUrl) ? coverImageUrl : null,
						    // Edition number
						    EditionNumber = bookEdition.EditionNumber,
						    // ISBN
						    Isbn = bookEdition.Isbn,
						    // Edition title
						    EditionTitle = bookEdition.EditionTitle,
						    // Summary
						    EditionSummary = bookEdition.Summary,
						    // Publication year
						    PublicationYear = bookEdition.PublicationYear,
						    // Page count
						    PageCount = bookEdition.PageCount,
						    // Language
						    Language = bookEdition.Language,
						    // Format
						    Format = bookEdition.Format,
						    // Publisher
						    Publisher = bookEdition.Publisher,
						    // Estimated price
						    EstimatedPrice = bookEdition.EstimatedPrice,
						    // Edition copies
						    BookEditionCopies = editionCopyBarcodes.ToList(),
						    // Authors
						    BookEditionAuthors = authorDtos != null && authorDtos.Any() 
							    ? authorDtos.Select(a => new BookEditionAuthorDto()
								    {
									    AuthorId = a.AuthorId
								    }).ToList()
							    : null!,
						    // Library shelf 
						    ShelfId = shelfDto?.ShelfId,
						    // Book edition inventory
						    BookEditionInventory = new()
						    {
							    TotalCopies = editionCopyBarcodes.Count, // Count total copies
							    AvailableCopies = 0,
							    BorrowedCopies = 0,
							    RequestCopies = 0,
							    ReservedCopies = 0
						    },
						    
						    // Default values
						    IsTrained = false,
						    IsDeleted = false,
						    CanBorrow = false,
						    Status = BookEditionStatus.Draft,
					    });
				    }

				    if (editionList.Any())
				    {
					    // Check for book code existence in DB
					    if ((await _bookService.Value.GetWithSpecAsync(new BaseSpecification<Book>(
						        b => b.BookCode.ToLower() == record.Key.ToLower()))).Data is BookDto bookDto) // already exist
					    {
						    // Add book id
						    editionList.ForEach(e => e.BookId = bookDto.BookId);
						    // Add range editions
						    await _unitOfWork.Repository<BookEdition, int>().AddRangeAsync(_mapper.Map<List<BookEdition>>(editionList));
					    }
					    else // not exist yet
					    {
						    // Extract all categories of first edition 
						    var editionCategoryNames = record.Values.First().Categories
							    .Split(", ")
							    .Select(x => x.Trim())
							    .ToList();
						    // Get all categories by name
						    var categoryDtos = 
							    (await _cateService.GetAllWithSpecAsync(new BaseSpecification<Category>(
							    c => editionCategoryNames.Contains(c.EnglishName)))
							    ).Data as IEnumerable<CategoryDto>;
						    // Process add new editions with book
						    bookDto = new BookDto()
						    {
							    // Title
							    Title = editionList.First().EditionTitle ?? string.Empty,
							    // Summary
							    Summary = editionList.First().EditionSummary,
							    // Book code
							    BookCode = record.Key,
							    // Book editions
							    BookEditions = editionList,
							    // Book categories
							    BookCategories = categoryDtos?.Select(cate => new BookCategoryDto()
							    {
								    CategoryId = cate.CategoryId
							    }).ToList() ?? null!,
							    
							    // Default value
							    IsDeleted = false,
							    BookCodeForAITraining = Guid.NewGuid()
						    };
						    
						    // Add new book
						    await _unitOfWork.Repository<Book, int>().AddAsync(_mapper.Map<Book>(bookDto));
					    }
					    
					    // Save change to DB
					    if(await _unitOfWork.SaveChangesAsync() > 0) totalImported = editionList.Count;
					    else failRecords.AddRange(record.Values);
				    }
			    }
		    }
		    
		    // Aggregate all book editions fail to upload & fail to save DB (if any)
		    failRecords.AddRange(readResp.Records
			    .Where(r => uploadFailList.Contains(r.CoverImage))
			    .ToList());
		    if (failRecords.Any()) totalFailed = failRecords.Count;
			
		    string message;
		    byte[]? fileBytes;
			// Generate a message based on the import and failure counts
		    if (totalImported > 0 && totalFailed == 0)
		    {
			    // All records imported successfully
			    message = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005), totalImported.ToString());
			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message);
		    }

		    if (totalImported > 0 && totalFailed > 0)
		    {
			    // Partial success with some failures
			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadBooks");

			    var baseMessage = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
			    var failMessage = lang == SystemLanguage.English
				    ? $", {totalFailed} failed to import"
				    : $", {totalFailed} thêm mới thất bại";

			    message = StringUtils.Format(baseMessage, totalImported.ToString()) + failMessage;
			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
		    }

		    if (totalImported == 0 && totalFailed > 0)
		    {
			    // Complete failure
			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadBooks");
			    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
			    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message, Convert.ToBase64String(fileBytes));
		    }

			// Default case: No records imported or failed
		    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
		    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message);
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process import book editions");
	    }
    }

    public async Task<IServiceResult> ExportAsync(ISpecification<BookEdition> spec)
    {
	    try
	    {
		    // Try to parse specification to BookEditionSpecification
		    var bookSpec = spec as BookEditionSpecification;
		    // Check if specification is null
		    if (bookSpec == null)
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }				
			
		    // Apply include
		    bookSpec.ApplyInclude(q => q
			    .Include(be => be.Shelf)
			    .Include(be => be.Book)
				    .ThenInclude(b => b.BookCategories)
						.ThenInclude(bc => bc.Category)
			    .Include(be => be.BookEditionAuthors)
					.ThenInclude(bea => bea.Author)
			    .Include(be => be.BookEditionCopies)
		    );
		    // Get all with spec
		    var entities = await _unitOfWork.Repository<BookEdition, int>()
			    .GetAllWithSpecAsync(bookSpec, tracked: false);
		    if (entities.Any()) // Exist data
		    {
			    // Map entities to dtos 
			    var bookEditionDtos = _mapper.Map<List<BookEditionDto>>(entities);
			    // Process export data to file
			    var fileBytes = CsvUtils.ExportToExcel(
				    bookEditionDtos.ToBookEditionCsvRecords());

			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				    fileBytes);
		    }
			
		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process export book editions");
	    }
    }
    
    private async Task<Dictionary<int, List<string>>> DetectWrongDataAsync(
	    List<BookEditionCsvRecord> records,
	    List<string> coverImageNames,
	    string[]? scanningFields,
	    SystemLanguage lang)
	{
	    // Check system lang
	    var isEng = lang == SystemLanguage.English;

	    // Initialize dictionary to hold errors
	    var errorMessages = new Dictionary<int, List<string>>();
	    // Initialize hashset to check uniqueness
	    var coverImageSet = new HashSet<string>();
	    var editionNumbers = new HashSet<int>();
	    var editionCopyBarcodes = new HashSet<string>();
	    var editionCategories = new Dictionary<string, string[]>();
	    // Default row index set to second row, as first row is header
	    var currDataRow = 2;

	    foreach (var record in records)
	    {
	        // Initialize error list for the current row
	        var rowErrors = new List<string>();

	        // Validate edition number uniqueness
	        if (!editionNumbers.Add(record.EditionNumber))
	        {
	            rowErrors.Add(isEng ? "Edition number must be unique" : "Số thứ thự ấn bản không được trùng");
	        }

	        // Validate existing ISBN
	        var isExistIsbn = await _unitOfWork.Repository<BookEdition, int>()
	            .AnyAsync(be => be.Isbn == ISBN.CleanIsbn(record.Isbn));
	        if (isExistIsbn)
	        {
	            rowErrors.Add(isEng ? $"ISBN '{record.Isbn}' already exists" : $"Mã ISBN '{record.Isbn}' đã tồn tại");
	        }

	        // Validate existing cover image
	        if (!coverImageNames.Exists(str => str.Equals(record.CoverImage)))
	        {
	            rowErrors.Add(isEng ? $"Image file name '{record.CoverImage}' does not exist" : $"Không tìm thấy file hình có tên '{record.CoverImage}'");
	        }

	        // Validate shelf location
	        var isExistShelfLocation = (await _libShelfService.AnyAsync(x => x.ShelfNumber == record.ShelfNumber)).Data is true;
	        if (!isExistShelfLocation)
	        {
	            rowErrors.Add(isEng ? $"Shelf number '{record.ShelfNumber}' does not exist" : $"Kệ số '{record.ShelfNumber}' không tồn tại");
	        }

	        // Validate author codes
	        var authorCodes = record.AuthorCodes.Split("\n");
	        if (authorCodes.Length < 1)
	        {
	            rowErrors.Add(isEng ? "Please add at least one author" : "Vui lòng thêm tác giả");
	        }
	        else
	        {
	            var duplicateCodes = authorCodes.GroupBy(x => x)
	                .Where(g => g.Count() > 1)
	                .Select(g => g.Key)
	                .ToList();

	            if (duplicateCodes.Any())
	            {
	                rowErrors.Add(isEng ? $"The following author codes are duplicated: {string.Join(", ", duplicateCodes)}" : $"Các mã tác giả sau đây bị trùng lặp: {string.Join(", ", duplicateCodes)}");
	            }

	            foreach (var authCode in authorCodes)
	            {
	                var isExist = (await _authorService.AnyAsync(x => x.AuthorCode != null 
	                                                                && x.AuthorCode.ToLower() == authCode.ToLower())).Data is true;
	                if (!isExist)
	                {
	                    rowErrors.Add(isEng ? $"Author code '{authCode}' does not exist" : $"Mã tác giả '{authCode}' không tồn tại");
	                }
	            }
	        }
			
		    // Check exist copy codes
		    var editionCopyLength = record.EditionCopyBarcodes != null ? record.EditionCopyBarcodes.Split("\n").Length : 0;
		    if (editionCopyLength > 0)
		    {
			    var copyCodes = record.EditionCopyBarcodes!.Split("\n");
			    var duplicateCodes = copyCodes.GroupBy(x => x)
				    .Where(g => g.Count() > 1)
				    .Select(g => g.Key)
				    .ToList();

			    // Check whether code is duplicate within a single cell
			    if (duplicateCodes.Any())
			    {
				   rowErrors.Add(isEng
						   ? $"The following barcodes are duplicated: {string.Join(", ", duplicateCodes)}"
						   : $"Các mã barcode sau đây bị trùng lặp: {string.Join(", ", duplicateCodes)}");
			    }
			    
			    // Check whether code is duplicate within all other cells
			    foreach (var code in copyCodes)
			    {
				    if (!editionCopyBarcodes.Add(code)) 
				    {
					    rowErrors.Add(isEng
						    ? $"Barcode '{code}' already exists in file"
						    : $"Barcode '{code}' bị trùng trong file");
				    }
			    }
			    
			    // Check whether barcode already exist in DB
			    foreach (var barcode in copyCodes)
			    {
				    var isExist = (await _editionCopyService.Value.AnyAsync(x => x.Barcode.ToLower() == barcode.ToLower())).Data is true;
				    if (isExist)
				    {
					    rowErrors.Add(isEng
						    ? $"Barcode '{barcode}' already not exist"
						    : $"Barcode '{barcode}' đã tồn tại");
				    }
			    }
		    }
		    
		    // Check exist categories
		    if (record.Categories.Split(",").Length < 1)
	        {
		        rowErrors.Add(isEng
			        ? "Please add at least one category"
			        : "Vui lòng thêm thể loại");
	        }
		    else
		    {
			    var categoryNames = record.Categories.Split(",").Select(x => x.Trim()).ToList();
			    var duplicateNames = categoryNames.GroupBy(x => x)
				    .Where(g => g.Count() > 1)
				    .Select(g => g.Key)
				    .ToList();

			    if (duplicateNames.Any())
			    {
				    rowErrors.Add(isEng
					    ? $"The following categories are duplicated: {string.Join(", ", duplicateNames)}"
					    : $"Các thể loại sau đây bị trùng lặp: {string.Join(", ", duplicateNames)}");
			    }

			    foreach (var cateName in categoryNames)
			    {
				    var isExist = (await _cateService.AnyAsync(x => x.EnglishName == cateName)).Data is true;
				    if (!isExist)
				    {
					    rowErrors.Add(isEng
						    ? $"Category '{cateName}' does not exist"
						    : $"Thể loại '{cateName}' không tồn tại");
				    }
			    }

			    // Check whether exist different categories in the same edition
			    if (editionCategories.TryGetValue(record.BookCode, out var defaultCategoryNames)) // Exist book code value
			    {
				    // Check whether having different categories (ignoring order)
				    if (!defaultCategoryNames.OrderBy(x => x).SequenceEqual(categoryNames.OrderBy(x => x)))
				    {
					    rowErrors.Add(isEng
						    ? "Book categories must be shared the same within different editions in a single book"
						    : "Các thể loại sách phải giống nhau trong các ấn bản khác nhau trong một cuốn sách");
				    }
			    }
			    else // Not exist yet
			    {
				    // Add list category name as default to compare with others
				    editionCategories.Add(record.BookCode, categoryNames.ToArray());
			    }
		    }
		    
			// Check duplicate ISBN
			var isbnList = records.Select(x => x.Isbn).ToList();
			var isIsbnDuplicate = isbnList.Count(x => x == record.Isbn) > 1;
			if (isIsbnDuplicate)
			{
				rowErrors.Add(isEng ? $"IBSN '{record.Isbn}' is duplicated" : $"Mã ISBN '{record.Isbn}' bị trùng");
			}
		    
			// Check duplicate edition number
			// Not allow to have same edition with existing edition
			var isDuplicateEditionNum = await _unitOfWork.Repository<BookEdition, int>().AnyAsync(be =>
				be.Book.BookCode == record.BookCode && // Whether in same book
				be.EditionNumber == record.EditionNumber); // Check for specific edition		
			if (isDuplicateEditionNum)
			{
				rowErrors.Add(isEng 
					? $"Edition number '{record.EditionNumber}' already exists in database with book code '{record.BookCode}'" 
					: $"Số thự tự ấn bản '{record.EditionNumber}' đã tồn tại trong cơ sở dữ liệu với book code '{record.BookCode}'");
			}
			
		    // Validations
		    // TODO: Validate book code
		    if (record.EditionTitle.Length > 150) // Edition title
		    {
			    rowErrors.Add(isEng
				    ? "Book edition title must not exceed than 150 characters"
				    : "Tiêu đề của ấn bản phải nhỏ hơn 150 ký tự");
		    }
		    if (record.Summary.Length > 500) // Edition summary
		    {
			    rowErrors.Add(isEng
				    ? "Edition summary must not exceed 500 characters"
				    : "Mô tả của ấn bản không vượt quá 500 ký tự");
		    }
		    if (record.EditionNumber <= 0 && record.PageCount < int.MaxValue) // Edition number
		    {
			    rowErrors.Add(isEng
				    ? "Book edition number is not valid"
				    : "Số thứ tự ấn bản không hợp lệ");
		    }
		    if (StringUtils.IsNumeric(record.Language) ||
		        StringUtils.IsDateTime(record.Language)) // Language
		    {
			    rowErrors.Add(isEng
				    ? "Language is not valid"
				    : "Ngôn ngữ không hợp lệ");
		    }
		    if (StringUtils.IsNumeric(record.Format) || StringUtils.IsDateTime(record.Format)
		        || !Enum.TryParse(typeof(BookFormat), record.Format, true, out _)) // Format
		    {
			    rowErrors.Add(isEng
				    ? "Book format is not valid"
				    : "Format sách không hợp lệ");
		    }
		    if (record.PageCount <= 0 && record.PageCount < int.MaxValue) // Page count
		    {
			    rowErrors.Add(isEng
				    ? "Page count is not valid"
				    : "Tổng số trang không hợp lệ");
		    }
		    if (!(int.TryParse(record.PublicationYear.ToString(), out var year) 
		        && year > 0 && year <= DateTime.Now.Year)) // Publication year
		    {
			    rowErrors.Add(isEng
				    ? "Publication year is not valid"
				    : "Năm xuất bản không hợp lệ");
		    }
		    if (StringUtils.IsNumeric(record.Publisher) || 
		        StringUtils.IsDateTime(record.Publisher)) // Publisher
		    {
			    rowErrors.Add(isEng
				    ? "Publisher is not valid"
				    : "Tên nhà xuất bản không hợp lệ");
		    }

		    if (record.EstimatedPrice < 1000 || record.EstimatedPrice > 9999999999)
		    {
			    if (record.EstimatedPrice < 1000)
			    {
				    rowErrors.Add(isEng
                        ? "EstimatedPrice must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND");
			    }
			    else if (record.EstimatedPrice > 9999999999)
			    {
				    rowErrors.Add(isEng
					    ? "EstimatedPrice exceeds the maximum limit of 9.999.999.999 VND"
					    : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND");
			    }
		    }
		    if (!ISBN.IsValid(record.Isbn, out _)) // Isbn
		    {
			    rowErrors.Add(isEng
	                ? "ISBN is not valid"
	                : "Mã ISBN không hợp lệ");
		    }
		    
		    if (scanningFields != null)
		    {
			    // Initialize book edition base spec
			    BaseSpecification<BookEdition>? editionBaseSpec = null;
			    // Initialize book base spec
			    BaseSpecification<Book>? bookBaseSpec = null;
			    // Initialize duplicate field
			    var isImageDuplicate = false;
			    
			    // Iterate each fields to add criteria scanning logic
			    foreach (var field in scanningFields)
			    {
				    var normalizedField = field.ToUpperInvariant();
				    
				    // Building query to check duplicates on BookEdition entity
				    var newEditionSpec = normalizedField switch
				    {
					    var editionTitle when editionTitle == nameof(BookEdition.EditionTitle).ToUpperInvariant() =>
						    new BaseSpecification<BookEdition>(e => e.EditionTitle != null && e.EditionTitle.Equals(record.EditionTitle)),
					    _ => null
				    };
				    
				    // Build query to check duplicates on Book entity
				    var newBookSpec = normalizedField switch
				    {
					    var bookCode when bookCode == nameof(Book.BookCode).ToUpperInvariant() =>
						    new BaseSpecification<Book>(b => b.BookCode.ToLower() == record.BookCode.ToLower()),
					    _ => null
				    };
				    
				    if (newEditionSpec != null) // Found new edition spec
				    {
					    // Combine specifications with AND logic
					    editionBaseSpec = editionBaseSpec == null
						    ? newEditionSpec
						    : editionBaseSpec.Or(newEditionSpec);
				    }
				    
				    if (newBookSpec != null) // Found new book spec
				    {
					    // Combine specifications with AND logic
					    bookBaseSpec = bookBaseSpec == null
						    ? newBookSpec
						    : bookBaseSpec.Or(newBookSpec);
				    }
				    
				    // Check whether existing scanning field (CoverImage) and cannot add to hashset due to duplicate
				    if (normalizedField == nameof(BookEdition.CoverImage).ToUpperInvariant()
				        && !coverImageSet.Add(record.CoverImage))
					{
						isImageDuplicate = true;
					}
			    }
			    
			    // Check exist with spec
			    if (editionBaseSpec != null && await _unitOfWork.Repository<BookEdition, int>().AnyAsync(editionBaseSpec))
			    {
				    rowErrors.Add(isEng ? $"Title '{record.EditionTitle}'" : $"Tên sách '{record.EditionTitle}' đã tồn tại");
			    }
			    if (bookBaseSpec != null && await _unitOfWork.Repository<Book, int>().AnyAsync(bookBaseSpec))
			    {
				    rowErrors.Add(isEng ? $"Book code '{record.BookCode}' already exists" : $"Mã sách '{record.BookCode}' đã tồn tại");
			    }
			    
			    // Check whether image is duplicate
			    if (isImageDuplicate)
			    {
				    rowErrors.Add(isEng ? 
					    $"Cover image '{record.CoverImage} is duplicated'" : 
					    $"Hình '{record.CoverImage}' bị trùng");
			    }
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
}