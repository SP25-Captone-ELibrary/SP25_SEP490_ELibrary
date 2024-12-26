using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Locations;
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
using Microsoft.EntityFrameworkCore;
using Serilog;
using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;
using Exception = System.Exception;
using UnprocessableEntityException = FPTU_ELibrary.Application.Exceptions.UnprocessableEntityException;

namespace FPTU_ELibrary.Application.Services;

public class BookEditionService : GenericService<BookEdition, BookEditionDto, int>, 
    IBookEditionService<BookEditionDto>
{
	private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
	private readonly ICloudinaryService _cloudService;

	public BookEditionService(
		ICloudinaryService cloudService,
	    ILibraryShelfService<LibraryShelfDto> libShelfService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) 
    : base(msgService, unitOfWork, mapper, logger)
	{
		_cloudService = cloudService;
	    _libShelfService = libShelfService;
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
                    Shelf = be.Shelf,
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
                        Title = be.Book.Title,
                        SubTitle = be.Book.SubTitle,
                        Summary = be.Book.Summary,
                        IsDeleted = be.Book.IsDeleted,
                        IsDraft = be.Book.IsDraft,
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
                dto = HandleEnumTextForSingleEdition(dto);
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
                    CreatedBy = be.CreatedBy,
                    Shelf = be.Shelf,
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
                        Title = be.Book.Title,
                        SubTitle = be.Book.SubTitle,
                        Summary = be.Book.Summary,
                        IsDeleted = be.Book.IsDeleted,
                        IsDraft = be.Book.IsDraft,
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
                bookEditionDtos = HandleEnumTextForEditions(bookEditionDtos);
                
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
				throw new UnprocessableEntityException("Invalid validations", errors);
			}

			// Check exist shelf location
			if (dto.ShelfId != null 
			    && int.TryParse(dto.ShelfId.ToString(), out var validShelfId)) // ShelfId must be numeric
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
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "book edition to process update" : "ấn bản để sửa"));
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
					.AnyAsync(be => be.Isbn == existingEntity.Isbn && // Any ISBN found 
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
			existingEntity.CanBorrow = dto.CanBorrow;
			existingEntity.EstimatedPrice = dto.EstimatedPrice;
			existingEntity.ShelfId = dto.ShelfId;
			
			// Check if there are any differences between the original and the updated entity
			if (!_unitOfWork.Repository<BookEdition, int>().HasChanges(existingEntity))
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
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

    private List<BookEditionDto> HandleEnumTextForEditions(List<BookEditionDto> dtos)
    {
        // Determine current system lang
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        // BookEditionCopyStatus enum descriptions
        var editionStatusDescriptions = Enum.GetValues(typeof(BookEditionCopyStatus))
             .Cast<BookEditionCopyStatus>()
             .ToDictionary(
                 e => e.ToString(),
                 e => EnumExtensions.GetDescription(e)
             );
        
        // BookFormat enum descriptions
         var formatDescriptions = Enum.GetValues(typeof(BookFormat))
            .Cast<BookFormat>()
            .ToDictionary(
                e => e.ToString(),
                e => EnumExtensions.GetDescription(e)
            );
        
         if (dtos.Any())
         {
            foreach (var edition in dtos)
            {
                if (edition.BookEditionCopies.Any())
                {
             	    foreach (var copy in edition.BookEditionCopies)
             	    {
             		    if (!string.IsNullOrEmpty(copy.Status) && editionStatusDescriptions.TryGetValue(copy.Status, out var statusDesc))
             		    {
             			    copy.Status = isEng ? StringUtils.AddWhitespaceToString(copy.Status) : statusDesc;
             		    }
             	    }
                }
                
                if (!string.IsNullOrEmpty(edition.Format) && formatDescriptions.TryGetValue(edition.Format, out var formatDesc))
                {
             	    edition.Format = isEng ? edition.Format : formatDesc;
                }
            }
         }
         
         return dtos;
    }

    private BookEditionDto HandleEnumTextForSingleEdition(BookEditionDto dto)
    {
        // Determine current system lang
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        // BookEditionCopyStatus enum descriptions
        var editionStatusDescriptions = Enum.GetValues(typeof(BookEditionCopyStatus))
             .Cast<BookEditionCopyStatus>()
             .ToDictionary(
                 e => e.ToString(),
                 e => EnumExtensions.GetDescription(e)
             );
        
        // BookFormat enum descriptions
        var formatDescriptions = Enum.GetValues(typeof(BookFormat))
            .Cast<BookFormat>()
            .ToDictionary(
                e => e.ToString(),
                e => EnumExtensions.GetDescription(e)
            );
         
         if (dto.BookEditionCopies.Any())
         {
             foreach (var copy in dto.BookEditionCopies)
             {
                 if (!string.IsNullOrEmpty(copy.Status) && editionStatusDescriptions.TryGetValue(copy.Status, out var statusDesc))
                 {
                     copy.Status = isEng ? StringUtils.AddWhitespaceToString(copy.Status) : statusDesc;
                 }
             }
         }
                
         if (!string.IsNullOrEmpty(dto.Format) && formatDescriptions.TryGetValue(dto.Format, out var formatDesc))
         {
             dto.Format = isEng ? dto.Format : formatDesc;
         }
         
         return dto;
    }
}