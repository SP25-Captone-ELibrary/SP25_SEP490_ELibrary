using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
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
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
    private readonly ICategoryService<CategoryDto> _categoryService;
    private readonly ICloudinaryService _cloudService;
    private readonly IBookEditionInventoryService<BookEditionInventoryDto> _inventoryService;
    private readonly IAuthorService<AuthorDto> _authorService;

    public BookEditionService(
        // Lazy service
        Lazy<IBookService<BookDto>> bookService,
        Lazy<IBookEditionAuthorService<BookEditionAuthorDto>> editionAuthorService,
        // Normal service
        IAuthorService<AuthorDto> authorService,
        IBookEditionInventoryService<BookEditionInventoryDto> inventoryService,
        ICloudinaryService cloudService,
        ILibraryShelfService<LibraryShelfDto> libShelfService,
        ICategoryService<CategoryDto> categoryService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
        _authorService = authorService;
        _bookService = bookService;
        _cloudService = cloudService;
        _libShelfService = libShelfService;
        _categoryService = categoryService;
        _inventoryService = inventoryService;
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

            // Check whether book edition in the trash bin
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
                customErrors.Add("editionNumber",
                    [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
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
                // Check exist edition copy code within DB
                var isCodeExist = await _unitOfWork.Repository<BookEditionCopy, int>()
                    .AnyAsync(x => x.Code == bec.Code);
                if (isCodeExist)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0006);
                    // Add errors
                    customErrors.Add($"bookCopies[{i}].code", [StringUtils.Format(errMsg, $"'{bec.Code}'")]);
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
                else if (errors.ContainsKey("bookEditionCopies")) errors.Remove("bookEditionCopies");

                if (errors.Any())
                {
                    throw new UnprocessableEntityException("Invalid validations", errors);
                }
            }

            // Check exist shelf location
            if (dto.ShelfId != null
                && int.TryParse(dto.ShelfId.ToString(), out var validShelfId) &&
                validShelfId > 0) // ShelfId must be numeric
            {
                var checkExistShelfRes = await _libShelfService.AnyAsync(x => x.ShelfId == validShelfId);
                if (checkExistShelfRes.Data is false)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg,
                            isEng ? "shelf location to process update" : "vị trí kệ sách để sửa"));
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
                    customErrs.Add("editionNumber",
                        [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
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
                    var isImageOnCloud =
                        (await _cloudService.IsExistAsync(updatePublicId, FileType.Image)).Data is true;
                    if (!isImageOnCloud)
                    {
                        isImageExist = false;
                    }
                }

                // Check if existing entity already has image
                if (!string.IsNullOrEmpty(existingEntity.CoverImage))
                {
                    // Extract public id from current entity
                    var currentPublicId = StringUtils.GetPublicIdFromUrl(existingEntity.CoverImage);
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

            // Check whether book edition contains any copy
            if (existingEntity.BookEditionCopies.Any())
            {
                return new ServiceResult(ResultCodeConst.Book_Warning0010,
                    await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0010));
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
            }

            // Check whether book edition contains any copy
            if (bookEditionList.SelectMany(be => be.BookEditionCopies).Any())
            {
                return new ServiceResult(ResultCodeConst.Book_Warning0010,
                    await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0010));
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

    public async Task<IServiceResult> UpdateTrainingStatusAsync(Guid trainingBookCode)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            var baseSpec =
                new BaseSpecification<BookEdition>(x => x.Book.BookCodeForAITraining.Equals(trainingBookCode));
            var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(baseSpec);

            foreach (var entity in bookEditionEntities)
            {
                entity.TrainedDay = DateTime.Now;
                entity.IsTrained = true;
                await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(entity);
            }

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
                        if (!string.IsNullOrEmpty(copy.Status) &&
                            editionStatusDescriptions.TryGetValue(copy.Status, out var statusDesc))
                        {
                            copy.Status = isEng ? StringUtils.AddWhitespaceToString(copy.Status) : statusDesc;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(edition.Format) &&
                    formatDescriptions.TryGetValue(edition.Format, out var formatDesc))
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
                if (!string.IsNullOrEmpty(copy.Status) &&
                    editionStatusDescriptions.TryGetValue(copy.Status, out var statusDesc))
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

    public async Task<IServiceResult> GetRelatedEditionWithMatchField(BookEditionDto dto, string fieldName)
    {
        // loại bỏ các edition có chung book id, chỉ lấy các edition của các đầu sách khác.
        var relatedEditions = new List<BookEditionDto>();
        if (fieldName.Equals(nameof(Author)))
        {
            var targetAuthorIds = dto.BookEditionAuthors
                .Select(bea => bea.AuthorId)
                .ToList();
            var sameAuthorEditionsQuery = new BaseSpecification<BookEdition>(be =>
                be.BookEditionId != dto.BookEditionId
                &&
                be.BookEditionAuthors.Any(ba => targetAuthorIds.Contains(ba.AuthorId))
            );

            sameAuthorEditionsQuery.ApplyInclude(q => q
                .Include(be => be.BookEditionAuthors)
                .ThenInclude(bea => bea.Author)
            );
            var result =
                (await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(
                    sameAuthorEditionsQuery)).ToList();
            relatedEditions = _mapper.Map<List<BookEditionDto>>(result);
        }

        if (fieldName.Equals(nameof(Category)))
        {
            var categorySpec = new BaseSpecification<Category>(c => c.BookCategories
                .Any(bc => bc.BookId == dto.Book.BookId));
            categorySpec.ApplyInclude(q => q.Include(c => c.BookCategories));
                var categories = (List<CategoryDto>)(await _categoryService.GetAllWithSpecAsync(categorySpec)).Data!; 
            var targetCategories = categories
                .Select(c => c.CategoryId)
                .ToList();
            var sameCategoryEditionsQuery = new BaseSpecification<BookEdition>(be =>
                be.BookEditionId != dto.BookEditionId &&
                be.Book.BookCategories
                    .Any(bc =>
                        targetCategories.Contains(bc.CategoryId)
                    )
            );
            // loại bỏ các edition có chung book id, chỉ lấy các edition của các đầu sách khác.
            // Apply Includes for Book, BookCategories, Category, and BookEditionAuthors
            sameCategoryEditionsQuery.ApplyInclude(q => q
                .Include(be => be.Book)
                .ThenInclude(b => b.BookCategories)
                .ThenInclude(c => c.Category));
            // Retrieve the data using the specification
            var result =
                (await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(
                    sameCategoryEditionsQuery)).ToList();
            relatedEditions = _mapper.Map<List<BookEditionDto>>(result);
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002)
            , relatedEditions);
    }
}