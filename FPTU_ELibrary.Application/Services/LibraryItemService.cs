using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Cloudinary;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemService : GenericService<LibraryItem, LibraryItemDto, int>,
    ILibraryItemService<LibraryItemDto>
{
	// Configure lazy service
	private readonly Lazy<ILibraryItemAuthorService<LibraryItemAuthorDto>> _editionAuthorService;
	private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _editionCopyService;
    private readonly Lazy<ILibraryItemGroupService<LibraryItemGroupDto>> _itemGroupService;
	private readonly Lazy<IElasticService> _elasticService;
	
	private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
	private readonly ICloudinaryService _cloudService;
	private readonly ICategoryService<CategoryDto> _cateService;
	private readonly IAuthorService<AuthorDto> _authorService;

	public LibraryItemService(
		// Lazy service
		Lazy<IElasticService> elasticService,
		Lazy<ILibraryItemAuthorService<LibraryItemAuthorDto>> editionAuthorService,
		Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> editionCopyService,
		Lazy<ILibraryItemGroupService<LibraryItemGroupDto>> itemGroupService,
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
		_cloudService = cloudService;
		_cateService = cateService;
	    _libShelfService = libShelfService;
	    _elasticService = elasticService;
        _itemGroupService = itemGroupService;
	    _editionCopyService = editionCopyService;
	    _editionAuthorService = editionAuthorService;
	}
	
    public override async Task<IServiceResult> CreateAsync(LibraryItemDto dto)
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

            // Select list of author ids
            var authorIds = dto.LibraryItemAuthors
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

            // Custom error responses
            var customErrors = new Dictionary<string, string[]>();
            // Initialize hash set of string to check unique of barcode
            var editionCopyCodes = new HashSet<string>();
            
            // Check exist category 
            var categoryDto = (await _cateService.GetByIdAsync(dto.CategoryId)).Data as CategoryDto;
            if (categoryDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "category" : "phân loại"));
            }
            
            // Check whether create with specific group
            if (dto.GroupId != null && dto.GroupId > 0) 
            {
                // Check exist group 
                // Build spec
                var groupSpec = new BaseSpecification<LibraryItemGroup>(g => g.GroupId == dto.GroupId);
                // Apply include
                groupSpec.ApplyInclude(q => q
                    .Include(g => g.LibraryItems));
                // Retrieve group by spec
                var groupDto = (await _itemGroupService.Value.GetWithSpecAsync(groupSpec)).Data as LibraryItemGroupDto;
                if (groupDto == null) // not found
                {
                    // Add error 
                    customErrors.Add(StringUtils.ToCamelCase(nameof(LibraryItem.GroupId)),
                        [isEng ? "Group is not exist" : "Không tìm thấy nhóm"]);
                }
                else // found 
                {
                    // Check whether same edition number within the same group
                    var isEditionNumberExist = groupDto.LibraryItems.Any(gi => gi.EditionNumber == dto.EditionNumber);
                    if (isEditionNumberExist)
                    {
                        // Add error 
                        customErrors.Add(StringUtils.ToCamelCase(nameof(LibraryItem.EditionNumber)),
                            [await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0002)]);
                    }
                }
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
                    // Add error
                    customErrors.Add(StringUtils.ToCamelCase(nameof(LibraryItemDto.CoverImage)),
                        [await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001)]);
                }
            }

            // Iterate each library item instance (if any) to check valid data
            var listItemInstance = dto.LibraryItemInstances.ToList();
            for (int i = 0; i < listItemInstance.Count; ++i)
            {
	            var iInstance = listItemInstance[i];

                if (editionCopyCodes.Add(iInstance.Barcode)) // Add to hash set string to ensure uniqueness
                {
	                // Check exist edition copy barcode within DB
	                var isCodeExist = await _unitOfWork.Repository<LibraryItemInstance, int>()
		                .AnyAsync(x => x.Barcode == iInstance.Barcode);
	                if (isCodeExist)
	                {
		                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0005);
		                // Add errors
		                customErrors.Add($"libraryItemInstances[{i}].barcode", [StringUtils.Format(errMsg, $"'{iInstance.Barcode}'")]);
	                }
                    else
                    {
                        // Try to validate with category prefix
                        var isValidBarcode = StringUtils.IsValidBarcodeWithPrefix(iInstance.Barcode, categoryDto.Prefix);
                        if (!isValidBarcode)
                        {
                            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0006);
                            // Add errors
                            customErrors.Add($"libraryItemInstances[{i}].barcode", 
                                [StringUtils.Format(errMsg, $"'{categoryDto.Prefix}'")]);
                        }
                    }
                }
                else // Duplicate found
                {
                    // Add error 
                    customErrors.Add(
                    	$"libraryItemInstances[{i}].barcode", 
                    	[isEng ? $"Barcode '{iInstance.Barcode}' is duplicated" : $"Số đăng ký cá biệt '{iInstance.Barcode}' đã bị trùng"]);
                }
                    
	            // Default status
	            iInstance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
	            // Boolean 
	            iInstance.IsDeleted = false;
            }

            // Iterate each library resource (if any) to check valid data
            var listResource = dto.LibraryItemResources.Select(lir => lir.LibraryResource).ToList();
            for (int i = 0; i < listResource.Count; ++i)
            {
                var lir = listResource[i];
                
                // Get file type
                Enum.TryParse(typeof(FileType), lir.FileFormat, out var fileType);
                // Check exist resource
                var checkExistResult = await _cloudService.IsExistAsync(lir.ProviderPublicId, (FileType)fileType!);
                if (checkExistResult.Data is false) // Return when not found resource on cloud
                {
                	// Add error
                    customErrors.Add($"libraryResources[{i}].resourceTitle",
                        [await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0003)]);
                } 
            }
            
            // Default value
            dto.IsTrained = false;
            dto.IsDeleted = false;
            dto.CanBorrow = false;
            // Clear ISBN hyphens
            dto.Isbn = !string.IsNullOrEmpty(dto.Isbn) ? ISBN.CleanIsbn(dto.Isbn) : dto.Isbn;
            // Check exist Isbn
            var isIsbnExist = await _unitOfWork.Repository<LibraryItem, int>()
                .AnyAsync(x => x.Isbn == dto.Isbn);
            if (isIsbnExist) // already exist 
            {
                // Add error
                customErrors.Add(
                    "isbn",
                    // Isbn already exist message
                    [await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0007)]);
            }

            // Any errors invoke when checking valid data
            if (customErrors.Any()) // exist errors
            {
                throw new UnprocessableEntityException("Invalid data", customErrors);
            }

            // Process create new book
            await _unitOfWork.Repository<LibraryItem, int>().AddAsync(_mapper.Map<LibraryItem>(dto));
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
            throw new Exception("Error invoke when process create new library item");
        }
    }
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<LibraryItem> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to LibraryItemSpecification
            var itemSpecification = specification as LibraryItemSpecification;
            // Check if specification is null
            if (itemSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total library items
            var totalLibItemWithSpec = await _unitOfWork.Repository<LibraryItem, int>().CountAsync(itemSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalLibItemWithSpec / itemSpecification.PageSize);

            // Set pagination to specification after count total library item
            if (itemSpecification.PageIndex > totalPage
                || itemSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                itemSpecification.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            itemSpecification.ApplyPaging(
                skip: itemSpecification.PageSize * (itemSpecification.PageIndex - 1),
                take: itemSpecification.PageSize);

            // Get all with spec and selector
            var libraryItems = await _unitOfWork.Repository<LibraryItem, int>()
                .GetAllWithSpecAndSelectorAsync(itemSpecification, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList()
                });

            if (libraryItems.Any()) // Exist data
            {
                // Convert to dto collection
                var itemDtos = _mapper.Map<List<LibraryItemDto>>(libraryItems);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryItemDto>(itemDtos,
                    itemSpecification.PageIndex, itemSpecification.PageSize, totalPage, totalLibItemWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<LibraryItemDto>>(libraryItems));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke process when get all library item");
        }
    }
    
// 	
//     public override async Task<IServiceResult> UpdateAsync(int id, LibraryItemDto dto)
//     {
// 		try
// 		{
// 			// Determine current lang context
// 			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 				LanguageContext.CurrentLanguage);
// 			var isEng = lang == SystemLanguage.English;
// 			
// 			// Validate inputs using the generic validator
// 			var validationResult = await ValidatorExtensions.ValidateAsync(dto);
// 			// Check for valid validations
// 			if (validationResult != null && !validationResult.IsValid)
// 			{
// 				// Convert ValidationResult to ValidationProblemsDetails.Errors
// 				var errors = validationResult.ToProblemDetails().Errors;
// 				
// 				// Ignores authors, edition copies
// 				if (errors.ContainsKey("bookEditionAuthors")) errors.Remove("bookEditionAuthors");
// 				else if(errors.ContainsKey("bookEditionCopies")) errors.Remove("bookEditionCopies");
//
// 				if (errors.Any())
// 				{
// 					throw new UnprocessableEntityException("Invalid validations", errors);
// 				}
// 			}
//
// 			// Check exist shelf location
// 			if (dto.ShelfId != null 
// 			    && int.TryParse(dto.ShelfId.ToString(), out var validShelfId) && validShelfId > 0) // ShelfId must be numeric
// 			{
// 				var checkExistShelfRes = await _libShelfService.AnyAsync(x => x.ShelfId == validShelfId);
// 				if (checkExistShelfRes.Data is false)
// 				{
// 					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
// 						StringUtils.Format(errMsg, isEng ? "shelf location to process update" : "vị trí kệ sách để sửa"));
// 				}
// 			}		
// 			
// 			// Retrieve the entity
// 			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetByIdAsync(id);
// 			if (existingEntity == null || existingEntity.IsDeleted)
// 			{
// 				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
// 					StringUtils.Format(errMsg, isEng ? "library item to process update" : "tài liệu để sửa"));
// 			}
// 			
// 			// Require transitioning to Draft status to modify or soft-delete a book
// 			if (existingEntity.Status != LibraryItemStatus.Draft)
// 			{
// 				return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
// 					await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
// 			}
// 			
// 			// Initialize custom errors
// 			var customErrs = new Dictionary<string, string[]>();
// 			
// 			// Check duplicate edition number (if change)
// 			if (!Equals(existingEntity.EditionNumber, dto.EditionNumber))
// 			{
// 				var isEditionNumDuplicate = await _unitOfWork.Repository<BookEdition, int>()
// 					.AnyAsync(x => x.EditionNumber == dto.EditionNumber && // Any other edition number match 
// 					               x.BookId == existingEntity.BookId); // from book that possessed the edition
// 				if (isEditionNumDuplicate)
// 				{
// 					customErrs.Add("editionNumber", [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
// 				}
// 			}
// 			
// 			// Check exist isbn (if change)
// 			if (!Equals(existingEntity.Isbn, dto.Isbn))
// 			{
// 				var isIsbnExist = await _unitOfWork.Repository<BookEdition, int>()
// 					.AnyAsync(be => be.Isbn == dto.Isbn && // Any ISBN found 
// 					                be.LibraryItemId != id); // Except request library item
// 				if (isIsbnExist)
// 				{
// 					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0007);
// 					customErrs.Add("isbn", [StringUtils.Format(errMsg, $"'{dto.Isbn}'")]);
// 				}
// 			}
// 			
// 			// Check exist cover image
// 			if (!Equals(existingEntity.CoverImage, dto.CoverImage) // Detect as cover image change 
// 			    && !string.IsNullOrEmpty(dto.CoverImage)) 
// 			{
// 				// Initialize field
// 				var isImageExist = true;
// 				
// 				// Extract public id from update entity
// 				var updatePublicId = StringUtils.GetPublicIdFromUrl(dto.CoverImage);
// 				if (string.IsNullOrEmpty(updatePublicId)) // Provider public id must be existed
// 				{
// 					isImageExist = false;
// 				}
// 				else // Exist public id
// 				{
// 					// Check existence on cloud
// 					var isImageOnCloud = (await _cloudService.IsExistAsync(updatePublicId, FileType.Image)).Data is true;
// 					if (!isImageOnCloud)
// 					{
// 						isImageExist = false;
// 					}
// 				}
// 				
// 				// Check if existing entity already has image
// 				if (!string.IsNullOrEmpty(existingEntity.CoverImage))
// 				{
// 					// Extract public id from current entity
// 					var currentPublicId	= StringUtils.GetPublicIdFromUrl(existingEntity.CoverImage);
// 					if (!Equals(currentPublicId, updatePublicId)) // Error invoke when update provider update id 
// 					{
// 						// Mark as fail to update
// 						return new ServiceResult(ResultCodeConst.SYS_Fail0003,
// 							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
// 					}
// 				}
//
// 				if (!isImageExist) // Invoke error image not found
// 				{
// 					// Return as not found image resource
// 					return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
// 						await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
// 				}
// 			}
// 			
// 			// Check if any errors invoke
// 			if (customErrs.Any())
// 			{
// 				throw new UnprocessableEntityException("Invalid data", customErrs);
// 			}
// 			
// 			// Process update entity
// 			existingEntity.Title = dto.EditionTitle;
// 			existingEntity.EditionSummary = dto.EditionSummary;
// 			existingEntity.EditionNumber = dto.EditionNumber;
// 			existingEntity.PublicationYear = dto.PublicationYear;
// 			existingEntity.Publisher = dto.Publisher;
// 			existingEntity.PageCount = dto.PageCount;
// 			existingEntity.Language = dto.Language;
// 			existingEntity.CoverImage = dto.CoverImage;
// 			existingEntity.Format = dto.Format;
// 			existingEntity.Isbn = dto.Isbn;
// 			existingEntity.EstimatedPrice = dto.EstimatedPrice;
// 			
// 			// Only process update shelf when valid
// 			if (dto.ShelfId > 0)
// 			{
// 				existingEntity.ShelfId = dto.ShelfId;
// 			}
// 			
// 			// Progress update when all require passed
// 			await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(existingEntity);
//
// 			// Save changes to DB
// 			var rowsAffected = await _unitOfWork.SaveChangesAsync();
// 			if (rowsAffected == 0)
// 			{
// 				return new ServiceResult(ResultCodeConst.SYS_Fail0003,
// 					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
// 			}
//
// 			// Mark as update success
// 			return new ServiceResult(ResultCodeConst.SYS_Success0003,
// 				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
// 		}
// 		catch (UnprocessableEntityException)
// 		{
// 			throw;
// 		}
// 		catch (Exception ex)
// 		{
// 			_logger.Error(ex.Message);
// 			throw;
// 		}
//     }
// 		
//     public override async Task<IServiceResult> DeleteAsync(int id)
//     {
// 	    try
// 	    {
// 		    // Determine current lang 
// 		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 			    LanguageContext.CurrentLanguage);
// 		    var isEng = lang == SystemLanguage.English;
// 			    
// 		    // Build a base specification to filter by BookEditionId
// 		    var baseSpec = new BaseSpecification<BookEdition>(a => a.LibraryItemId == id);
// 			// Apply including authors
// 			baseSpec.ApplyInclude(q => q
// 				.Include(be => be.BookEditionAuthors));
// 		    
// 		    // Retrieve library item with specification
// 		    var bookEditionEntity = await _unitOfWork.Repository<BookEdition, int>().GetWithSpecAsync(baseSpec);
// 		    if (bookEditionEntity == null)
// 		    {
// 			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
// 				    StringUtils.Format(errMsg, isEng ? "library item to process delete" : "tài liệu để xóa"));
// 		    }
//
// 		    // Check whether library item in the trash bin or not in draft status
// 		    if (!bookEditionEntity.IsDeleted)
// 		    {
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
// 		    }
//
//             // Try to delete all library item authors (if any)
//             if (bookEditionEntity.BookEditionAuthors.Any())
//             {
//                 // Process delete range without save changes
//                 await _editionAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
//                     // Select all existing bookEditionAuthorId
//                     bookEditionEntity.BookEditionAuthors.Select(ba => ba.BookEditionAuthorId).ToArray());
//             }
//
//             // Perform delete library item, and delete cascade with BookEditionInventory
//             await _unitOfWork.Repository<BookEdition, int>().DeleteAsync(id);
//
//             // Save to DB
//             if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
//             {
//                 return new ServiceResult(ResultCodeConst.SYS_Success0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
//             }
//
//             // Fail to delete
//             return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
//         }
//         catch (DbUpdateException ex)
//         {
//             if (ex.InnerException is SqlException sqlEx)
//             {
//                 switch (sqlEx.Number)
//                 {
//                     case 547: // Foreign key constraint violation
//                         return new ServiceResult(ResultCodeConst.SYS_Fail0007,
//                             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
//                 }
//             }
//
//             // Throw if other issues
//             throw;
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when progress delete data");
//         }
//     }
// 	

     public async Task<IServiceResult> GetDetailAsync(int id)
     {
         try
         {
             // Build specification
             var baseSpec = new BaseSpecification<LibraryItem>(b => b.LibraryItemId == id);
             var itemEntity = await _unitOfWork.Repository<LibraryItem, int>()
                 .GetWithSpecAndSelectorAsync(baseSpec, be => new LibraryItem()
                 {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList(),
                    LibraryItemResources = be.LibraryItemResources.Select(lir => new LibraryItemResource()
                    {
                       LibraryItemResourceId = lir.LibraryItemResourceId,
                       LibraryItemId = lir.LibraryItemId,
                       ResourceId = lir.ResourceId,
                       LibraryResource = lir.LibraryResource
                    }).ToList(),
                 });

             if (itemEntity != null)
             {
                 // Map to dto
                 var dto = _mapper.Map<LibraryItemDto>(itemEntity);
                 
                 // Convert to library item detail dto
                 var itemDetailDto = dto.ToLibraryItemDetailDto();

                 return new ServiceResult(ResultCodeConst.SYS_Success0002,
                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), itemDetailDto);
             }

             return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
         }
         catch (Exception ex)
         {
             _logger.Error(ex.Message);
             throw new Exception("Error invoke when get library item by id");
         }
     }
     
//
//     public async Task<IServiceResult> GetRelatedEditionWithMatchFieldAsync(LibraryItemDto dto, string fieldName)
//     {
//         // loại bỏ các edition có chung book id, chỉ lấy các edition của các đầu sách khác.
//         var relatedEditions = new List<LibraryItemDto>();
//         if (fieldName.Equals(nameof(Author)))
//         {
//             var targetAuthorIds = dto.BookEditionAuthors
//                 .Select(bea => bea.AuthorId)
//                 .ToList();
//             var sameAuthorEditionsQuery = new BaseSpecification<BookEdition>(be =>
//                 be.LibraryItemId != dto.BookEditionId
//                 &&
//                 be.BookEditionAuthors.Any(ba => targetAuthorIds.Contains(ba.AuthorId))
//             );
//
//             sameAuthorEditionsQuery.ApplyInclude(q => q
//                 .Include(be => be.BookEditionAuthors)
//                 .ThenInclude(bea => bea.Author)
//             );
//             var result =
//                 (await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(
//                     sameAuthorEditionsQuery)).ToList();
//             relatedEditions = _mapper.Map<List<LibraryItemDto>>(result);
//         }
//
//         if (fieldName.Equals(nameof(Category)))
//         {
//             var categorySpec = new BaseSpecification<Category>(c => c.BookCategories
//                 .Any(bc => bc.BookId == dto.Book.BookId));
//             categorySpec.ApplyInclude(q => q.Include(c => c.BookCategories));
//                 var categories = (List<CategoryDto>)(await _cateService.GetAllWithSpecAsync(categorySpec)).Data!; 
//             var targetCategories = categories
//                 .Select(c => c.CategoryId)
//                 .ToList();
//             var sameCategoryEditionsQuery = new BaseSpecification<BookEdition>(be =>
//                 be.LibraryItemId != dto.BookEditionId &&
//                 be.Book.BookCategories
//                     .Any(bc =>
//                         targetCategories.Contains(bc.CategoryId)
//                     )
//             );
//             // loại bỏ các edition có chung book id, chỉ lấy các edition của các đầu sách khác.
//             // Apply Includes for Book, BookCategories, Category, and BookEditionAuthors
//             sameCategoryEditionsQuery.ApplyInclude(q => q
//                 .Include(be => be.Book)
//                 .ThenInclude(b => b.BookCategories)
//                 .ThenInclude(c => c.Category));
//             // Retrieve the data using the specification
//             var result =
//                 (await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(
//                     sameCategoryEditionsQuery)).ToList();
//             relatedEditions = _mapper.Map<List<LibraryItemDto>>(result);
//         }
//
//         return new ServiceResult(ResultCodeConst.SYS_Success0002,
//             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002)
//             , relatedEditions);
//     }
//     
//     public async Task<IServiceResult> UpdateStatusAsync(int id)
//     {
// 	    try
// 	    {
// 		    // Determine current system lang
// 		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 			    LanguageContext.CurrentLanguage);
// 		    var isEng = lang == SystemLanguage.English;
// 		    
// 		    // Build spec
// 		    var baseSpec = new BaseSpecification<BookEdition>(x => x.LibraryItemId == id);
// 			// Apply include
// 			baseSpec.ApplyInclude(q => q
// 				// Include edition inventory
// 				.Include(x => x.BookEditionInventory)
// 				// Include book
// 				.Include(x => x.Book)
// 				// Include book categories
// 				.ThenInclude(x => x.BookCategories)
// 				// Include category
// 				.ThenInclude(x => x.Category)
// 				// Include library item authors
// 				.Include(x => x.BookEditionAuthors)
// 				// Include author
// 				.ThenInclude(bea => bea.Author)
// 				// Include library item copies
// 				.Include(x => x.BookEditionCopies)
// 				// Include library shelf
// 				.Include(x => x.Shelf)!
// 			);
// 			
// 		    // Retrieve library item with specific ID 
// 			var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetWithSpecAsync(baseSpec);
// 			if (existingEntity == null || existingEntity.IsDeleted) // Check whether book exist or marking as deleted
// 			{
// 				var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
// 					StringUtils.Format(errMSg, isEng ? "library item" : "tài liệu"));
// 			}
// 			
// 			// Check current library item status
// 			if (existingEntity.Status == LibraryItemStatus.Draft) // Draft -> Published
// 			{
// 				// Initialize err msg
// 				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0012);
// 				// Validate edition information before published
// 				// Check for shelf location
// 				if (existingEntity.ShelfId == null || existingEntity.ShelfId == 0)
// 				{
// 					return new ServiceResult(ResultCodeConst.Book_Warning0012,
// 						StringUtils.Format(errMsg, isEng 
// 							? "Shelf location not found" : "Không tìm thấy vị trí kệ cho sách"));
// 				}
// 				if (existingEntity.ShelfId > 0)
// 				{
// 					// Check for exist at least one library item copy that mark as in shelf
// 					if (existingEntity.BookEditionCopies.All(x => x.Status != nameof(LibraryItemInstanceStatus.InShelf)))
// 					{
// 						return new ServiceResult(ResultCodeConst.Book_Warning0012,
// 							StringUtils.Format(errMsg, isEng 
// 								? "Required at least one copy in shelf" 
// 								: "Cần ít nhất một bản in có sẵn trên kệ"));
// 					}	
// 				}
// 				
// 				// Process change status
// 				existingEntity.Status = LibraryItemStatus.Published;
//
// 				// Process update change to DB
// 				if (await _unitOfWork.SaveChangesAsync() > 0) // Success
// 				{
// 					// Initialize bool field
// 					var isAddToElastic = false;
// 					// Synchronize data to ElasticSearch
//                     if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.LibraryItemIndex))
//                     {
// 	                    // Convert to BookEditionDto
// 	                    var dto = _mapper.Map<LibraryItemDto>(existingEntity);
// 	                    
// 	                    // Check whether exist any document
// 	                    var isExistAnyDocument = await _elasticService.Value.DocumentExistsAsync<ElasticBook>(
// 		                    documentId: existingEntity.BookId.ToString());
//
// 	                    if (!isExistAnyDocument) // Not found
// 	                    {
// 		                    // Process add both root and nested object
// 		                    isAddToElastic = await _elasticService.Value.AddOrUpdateAsync(
// 			                    document: dto.ToElasticBook(),
// 			                    documentKeyName: nameof(ElasticBook.BookId)); // Custom elastic _id with BookId value
// 	                    }
// 	                    else // Add only nested object
// 	                    {
// 		                    // Get nested object fields name 
// 		                    var nestedFieldName = ElasticUtils.GetElasticFieldName<ElasticBook>(nameof(ElasticBook.BookEditions)) ?? string.Empty;
// 		                    // Get nested object key name
// 		                    var nestedKey = ElasticUtils.GetElasticFieldName<ElasticLibraryItem>(nameof(ElasticLibraryItem.LibraryItemId)) ?? string.Empty;
// 		                    
//                     		// Add new book to elastic
//                     		isAddToElastic = await _elasticService.Value.AddOrUpdateNestedAsync<ElasticBook, ElasticLibraryItem>(
// 			                    documentId: existingEntity.BookId.ToString(),
// 			                    nestedFieldName: nestedFieldName,
// 								nestedObject: dto.ToElasticBookEdition(),
// 			                    nestedKey: nestedKey,
// 			                    nestedKeyValue: existingEntity.LibraryItemId.ToString());
// 	                    }
//                     }
//                     
//                     // Custom message for failing to synchronize data to elastic
//                     var msg = !isAddToElastic 
// 	                    ? isEng ? ", but fail to add data to Elastic" : ", nhưng cập nhật dữ liệu mới vào Elastic thất bại"
// 	                    : string.Empty;
//                     return new ServiceResult(ResultCodeConst.SYS_Success0003,
// 	                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
// 				}
// 			}
// 			else if (existingEntity.Status == LibraryItemStatus.Published) // Published -> Draft
// 			{
// 				// Initialize err msg
// 				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0013);
// 				// Check whether book in borrow status (One or more copy now in store in library shelf) 
// 				if (existingEntity.CanBorrow) 
// 				{
// 					// Do not allow to change status
// 					return new ServiceResult(ResultCodeConst.Book_Warning0013,
// 						StringUtils.Format(errMsg, isEng ? "There still exist copy in shelf" : "Vẫn còn sách ở trên kệ"));
// 				}
// 				
// 				// Check inventory total whether to allow change status to Draft
// 				if (existingEntity.BookEditionInventory != null &&
// 				    (existingEntity.BookEditionInventory.RequestCopies > 0 ||
// 				     existingEntity.BookEditionInventory.BorrowedCopies > 0 ||
// 				     existingEntity.BookEditionInventory.ReservedCopies > 0))
// 				{
// 					// Cannot change data that is on borrowing or reserved
// 					return new ServiceResult(ResultCodeConst.Book_Warning0013,
//                     	StringUtils.Format(errMsg, isEng 
// 						? "Copy is on borrowing or reserved" 
// 						: "Bản in đang được mượn hoặc được đặt trước"));
// 				}
// 				
// 				// Process change status
// 				existingEntity.Status = LibraryItemStatus.Draft;
//
// 				// Process update change to DB
// 				if (await _unitOfWork.SaveChangesAsync() > 0) // Success
// 				{
// 					// Initialize bool field
// 					var isDeletedNested = false;
// 					// Progress delete data in Elastic
// 					if (await _elasticService.Value.CreateIndexIfNotExistAsync(ElasticIndexConst.LibraryItemIndex))
// 					{
// 						// Get nested object fields name 
// 						var nestedFieldName = ElasticUtils.GetElasticFieldName<ElasticBook>(nameof(ElasticBook.BookEditions)) ?? string.Empty;
// 						// Get nested object key name
// 						var nestedKey = ElasticUtils.GetElasticFieldName<ElasticLibraryItem>(nameof(ElasticLibraryItem.LibraryItemId)) ?? string.Empty;
// 						
// 						// Check whether the nested object exist
// 						var isNestedObjectExist = await _elasticService.Value.NestedExistAsync<ElasticLibraryItem>(
// 							documentId: existingEntity.BookId.ToString(),
// 							nestedFieldName: nestedFieldName,
// 							nestedKey: nestedKey,
// 							nestedKeyValue: existingEntity.LibraryItemId.ToString());
//
// 						if (isNestedObjectExist) // Is Exist
// 						{
// 							// Process delete nested object
// 							isDeletedNested = await _elasticService.Value.DeleteNestedAsync<ElasticBook>(
// 								documentId: existingEntity.BookId.ToString(),
// 								nestedFieldName: nestedFieldName,
// 								nestedKey: nestedKey,
// 								nestedKeyValue: existingEntity.LibraryItemId.ToString());
// 						}
// 					}
// 					
// 					// Custom message for failing to synchronize data to elastic
// 					var msg = !isDeletedNested 
// 						? isEng ? ", but fail to delete Elastic data" : ", nhưng xóa dữ liệu Elastic thất bại"
// 						: string.Empty;
// 					return new ServiceResult(ResultCodeConst.SYS_Success0003,
// 						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + msg);
// 				}
// 			}
// 			
// 			// Mark as fail to save
// 			return new ServiceResult(ResultCodeConst.SYS_Fail0003,
// 				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
// 	    }
// 	    catch (Exception ex)
// 	    {
// 		    _logger.Error(ex.Message);
// 		    throw new Exception("Error invoke when process update library item status");
// 	    }
//     }
//
//     public async Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow)
//     {
//         try
//         {
//             // Determine current system lang
//             var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
//                 LanguageContext.CurrentLanguage);
//             var isEng = lang == SystemLanguage.English;
//
//             // Retrieve library item by id
//             var existingEntity = await _unitOfWork.Repository<BookEdition, int>().GetByIdAsync(id);
//             if (existingEntity == null)
//             {
//                 var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
//                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
//                     StringUtils.Format(errMsg, isEng
//                         ? "library item to update borrow status"
//                         : "tài liệu để sửa trạng thái có thể mượn"), false);
//             }
//
//             // Update status
//             existingEntity.CanBorrow = canBorrow;
//
//             // Progress update without change 
//             await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(existingEntity);
//
//             // Mark as update success
//             return new ServiceResult(ResultCodeConst.SYS_Success0003,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process update edition borrow status");
//         }
//     }
//
    public async Task<IServiceResult> UpdateShelfLocationAsync(int id, int? shelfId)
    {
	    try
	    {	
		    // Determine current lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<LibraryItem>(x => x.LibraryItemId == id);
		    // Apply include 
		    baseSpec.ApplyInclude(q => q
			    .Include(be => be.LibraryItemInstances)
		    );
		    
			// Retrieve library item by id
			var existingEntity = await _unitOfWork.Repository<LibraryItem, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"), false);
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
				// Check whether item already assigned to current shelf location
				if (existingEntity.ShelfId == shelfId) // same shelf location
				{
					// Mark as update success
					return new ServiceResult(ResultCodeConst.SYS_Success0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
				}
			}
			
			// Required all book copy must be in out-of-shelf status
            if (existingEntity.LibraryItemInstances
                .Select(bec => bec.Status)
                .Any(status => status != nameof(LibraryItemInstanceStatus.OutOfShelf)))
            {
            	// Msg: Cannot process, please move all edition copy status to inventory first
            	return new ServiceResult(ResultCodeConst.LibraryItem_Warning0009,
            		await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0009));
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
		    throw new Exception("Error invoke when process update shelf location for library item");
	    }
    }
    
//     
//     public async Task<IServiceResult> UpdateTrainingStatusAsync(Guid trainingBookCode)
//     {
//         try
//         {
//             // Determine current lang context
//             var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
//                 LanguageContext.CurrentLanguage);
//             var isEng = lang == SystemLanguage.English;
//
//             var baseSpec =
//                 new BaseSpecification<BookEdition>(x => x.Book.BookCodeForAITraining.Equals(trainingBookCode));
//             var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>().GetAllWithSpecAsync(baseSpec);
//
//             foreach (var entity in bookEditionEntities)
//             {
//                 entity.TrainedDay = DateTime.Now;
//                 entity.IsTrained = true;
//                 await _unitOfWork.Repository<BookEdition, int>().UpdateAsync(entity);
//             }
//
//             // Save changes to DB
//             var rowsAffected = await _unitOfWork.SaveChangesAsync();
//             if (rowsAffected == 0)
//             {
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0003,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
//             }
//
//             // Mark as update success
//             return new ServiceResult(ResultCodeConst.SYS_Success0003,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
//         }
//         catch (UnprocessableEntityException)
//         {
//             throw;
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw;
//         }
//     }
//     
//     public async Task<IServiceResult> SoftDeleteAsync(int id)
//     {
// 	    try
// 	    {
// 		    // Determine current system lang 
// 		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 			    LanguageContext.CurrentLanguage);
// 		    var isEng = lang == SystemLanguage.English;
//
// 		    // Build spec
// 		    var baseSpec = new BaseSpecification<BookEdition>(x => x.LibraryItemId == id);
// 		    // Include all constraints to update soft delete
// 		    baseSpec.ApplyInclude(q => q
// 			    // Ignore include all authors, due to library item can exist without any book belongs to    
// 			    // Include all library item copy
// 			    .Include(be => be.BookEditionCopies)
// 		    );
// 		    // Get library item with spec
// 		    var existingEntity = await _unitOfWork.Repository<BookEdition, int>()
// 			    .GetWithSpecAsync(baseSpec);
// 		    // Check if library item already mark as deleted
// 		    if (existingEntity == null || existingEntity.IsDeleted)
// 		    {
// 			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
// 				    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
// 		    }
//
// 		    // Require transitioning to Draft status to modify or soft-delete a book
// 		    if (existingEntity.Status != LibraryItemStatus.Draft)
// 		    {
// 			    return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
// 		    }
//
// 		    // Check whether library item contains any copy, which mark as not deleted yet
// 		    if (existingEntity.BookEditionCopies.Any(x => !x.IsDeleted))
// 		    {
// 			    // Extract all current edition copy ids
// 			    var editionCopyIds = existingEntity.BookEditionCopies
// 				    .Where(bec => !bec.IsDeleted)
// 				    .Select(bec => bec.BookEditionCopyId).ToList();
// 			    if (editionCopyIds.Any()) // Found any copy is not deleted yet
// 			    {
// 				    // Try to soft delete all related edition copies
// 				    var deleteResult = await _editionCopyService.Value.SoftDeleteRangeAsync(
// 					    bookEditionId: existingEntity.LibraryItemId,
// 					    bookEditionCopyIds: editionCopyIds);
// 				    // Check whether fail to delete range library item copies
// 				    if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
// 			    }
// 		    }
//
// 		    // Update delete status
// 		    existingEntity.IsDeleted = true;
//
// 		    // Save changes to DB
// 		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
// 		    if (rowsAffected == 0)
// 		    {
// 			    // Get error msg
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
// 		    }
//
// 		    return new ServiceResult(ResultCodeConst.SYS_Success0007,
// 			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
// 	    }
// 	    catch (UnprocessableEntityException)
// 	    {
// 		    return new ServiceResult(ResultCodeConst.Book_Warning0008,
// 			    await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0008));
// 	    }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process soft delete library item");
//         }
//     }
//
//     public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
//     {
//         try
//         {
//             // Build spec
//             var baseSpec = new BaseSpecification<BookEdition>(x => ids.Contains(x.LibraryItemId));
//             // Include all constraints to update soft delete
//             baseSpec.ApplyInclude(q => q
//                 // Ignore include all authors, due to library item can exist without any book belongs to    
//                 // Include all library item copy
//                 .Include(be => be.BookEditionCopies)
//             );
//             var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
//                 .GetAllWithSpecAsync(baseSpec);
//             // Check if any data already soft delete
//             var bookEditionList = bookEditionEntities.ToList();
//             if (bookEditionList.Any(x => x.IsDeleted))
//             {
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
//             }else if (bookEditionList.Any(x => x.Status != LibraryItemStatus.Draft))
//             {
// 	            // Require transitioning to Draft status to modify or soft-delete a book
// 	            return new ServiceResult(ResultCodeConst.LibraryItem_Fail0002,
// 		            await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Fail0002));
//             }
//             
//             // Check whether library item contains any copy, which mark as not deleted yet
//             if (bookEditionList.SelectMany(be => be.BookEditionCopies).Any(bec => !bec.IsDeleted)) 
//             {
// 	            // Iterate each edition to soft delete all copies
// 	            foreach (var edition in bookEditionList)
// 	            {
// 		            // Extract all current edition copy ids
// 		            var editionCopyIds = edition.BookEditionCopies
// 			            .Where(bec => !bec.IsDeleted)
// 			            .Select(bec => bec.BookEditionCopyId).ToList();
// 		            if (editionCopyIds.Any())
// 		            {
// 			            // Try to soft delete all related edition copies
// 			            var deleteResult = await _editionCopyService.Value.SoftDeleteRangeAsync(
// 				            bookEditionId: edition.LibraryItemId, 
// 				            bookEditionCopyIds: editionCopyIds);
// 			            // Check whether fail to delete range library item copies
// 			            if (deleteResult.ResultCode != ResultCodeConst.SYS_Success0007) return deleteResult;
// 		            }
// 	            }
//             }
//
//             // Iterate each library item
//             foreach (var be in bookEditionList)
//             {
//                 // Update deleted status
//                 be.IsDeleted = true;
//             }
//
//             // Save changes to DB
//             var rowsAffected = await _unitOfWork.SaveChangesAsync();
//             if (rowsAffected == 0)
//             {
//                 // Get error msg
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
//             }
//
//             return new ServiceResult(ResultCodeConst.SYS_Success0007,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when remove range library item");
//         }
//     }
//
//     public async Task<IServiceResult> UndoDeleteAsync(int id)
//     {
// 	    try
// 	    {
// 		    // Determine current system lang 
// 		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 			    LanguageContext.CurrentLanguage);
// 		    var isEng = lang == SystemLanguage.English;
// 		    
// 		    // Build spec
// 		    var baseSpec = new BaseSpecification<BookEdition>(x => x.LibraryItemId == id);
// 		    // Include all constraints to update soft delete
// 		    baseSpec.ApplyInclude(q => q
// 			    // Ignore include all authors, due to library item can exist without any book belongs to    
// 			    // Include all library item copy
// 			    .Include(be => be.BookEditionCopies)
// 		    );
// 		    var existingEntity = await _unitOfWork.Repository<BookEdition, int>()
// 			    .GetWithSpecAsync(baseSpec);
// 		    // Check if library item already mark as deleted
// 		    if (existingEntity == null || !existingEntity.IsDeleted)
// 		    {
// 			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
// 			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
// 				    StringUtils.Format(errMsg, isEng ? "library item" : "tài liệu"));
// 		    }
// 				
// 		    // Check whether library item contains any copy, which mark as deleted
// 		    if (existingEntity.BookEditionCopies.Any(x => x.IsDeleted))
// 		    {
// 			    // Extract all current edition copy ids
// 			    var editionCopyIds = existingEntity.BookEditionCopies
// 				    .Where(bec => bec.IsDeleted)
// 				    .Select(bec => bec.BookEditionCopyId).ToList();
// 			    if (editionCopyIds.Any())
// 			    {
// 				    // Try to undo undo all related edition copies
// 				    var undoResult = await _editionCopyService.Value.UndoDeleteRangeAsync(
// 					    bookEditionId: existingEntity.LibraryItemId, 
// 					    bookEditionCopyIds: editionCopyIds);
// 				    // Check whether fail to undo range library item copies
// 				    if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
// 			    }
// 		    }
// 		    
// 		    // Update delete status
// 		    existingEntity.IsDeleted = false;
// 		    
// 		    // Save changes to DB
// 		    var rowsAffected = await _unitOfWork.SaveChangesAsync();
// 		    if (rowsAffected == 0)
// 		    {
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
// 		    }
//
//             // Mark as update success
//             return new ServiceResult(ResultCodeConst.SYS_Success0009,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process undo delete library item");
//         }
//     }
//
//     public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
//     {
//         try
//         {
//             // Build spec
//             var baseSpec = new BaseSpecification<BookEdition>(x => ids.Contains(x.LibraryItemId));
//             // Include all constraints to update soft delete
//             baseSpec.ApplyInclude(q => q
//                 // Ignore include all authors, due to library item can exist without any book belongs to    
//                 // Include all library item copy
//                 .Include(be => be.BookEditionCopies)
//             );
//             var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
//                 .GetAllWithSpecAsync(baseSpec);
//             // Check if any data already soft delete
//             var bookEditionList = bookEditionEntities.ToList();
//             if (bookEditionList.Any(x => !x.IsDeleted))
//             {
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
//             }
//             
//             // Check whether library item contains any copy, which mark as deleted
//             if (bookEditionList.SelectMany(be => be.BookEditionCopies).Any(be => be.IsDeleted))
//             {
// 	            // Iterate each edition to undo delete all copies
// 	            foreach (var edition in bookEditionList)
// 	            {
// 		            // Extract all current edition copy ids
// 		            var editionCopyIds = edition.BookEditionCopies
// 			            .Where(bec => bec.IsDeleted)
// 						.Select(bec => bec.BookEditionCopyId).ToList();
// 		            if (editionCopyIds.Any())
// 		            {
// 			            // Try to soft undo all related edition copies
// 			            var undoResult = await _editionCopyService.Value.UndoDeleteRangeAsync(
// 				            bookEditionId: edition.LibraryItemId, 
// 				            bookEditionCopyIds: editionCopyIds);
// 			            // Check whether fail to undo range library item copies
// 			            if (undoResult.ResultCode != ResultCodeConst.SYS_Success0009) return undoResult;
// 		            }
// 	            }
//             }
//
//             // Iterate each library item
//             foreach (var be in bookEditionList)
//             {
//                 // Update deleted status
//                 be.IsDeleted = false;
//             }
//
//             // Save changes to DB
//             var rowsAffected = await _unitOfWork.SaveChangesAsync();
//             if (rowsAffected == 0)
//             {
//                 // Get error msg
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), true);
//             }
//
//             // Mark as update success
//             return new ServiceResult(ResultCodeConst.SYS_Success0009,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process undo delete range");
//         }
//     }
//
//     public async Task<IServiceResult> DeleteRangeAsync(int[] ids)
//     {
//         try
//         {
//             // Get all matching library item 
//             // Build spec
//             var baseSpec = new BaseSpecification<BookEdition>(e => ids.Contains(e.LibraryItemId));
//             // Apply including authors
//             baseSpec.ApplyInclude(q => q
//                 .Include(be => be.BookEditionAuthors));
//             // Get all author with specification
//             var bookEditionEntities = await _unitOfWork.Repository<BookEdition, int>()
//                 .GetAllWithSpecAsync(baseSpec);
//             // Check if any data already soft delete
//             var bookEditionList = bookEditionEntities.ToList();
//             if (bookEditionList.Any(x => !x.IsDeleted))
//             {
//                 return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
//             }
//
//             // Try to clear all authors existing in each of library item (if any)
//             foreach (var be in bookEditionList)
//             {
//                 // Process delete range without save changes
//                 await _editionAuthorService.Value.DeleteRangeWithoutSaveChangesAsync(
//                     // Select all existing bookEditionAuthorId
//                     be.BookEditionAuthors.Select(ba => ba.BookEditionAuthorId).ToArray());
//             }
//
//             // Process delete range, and delete cascade with BookEditionInventory
//             await _unitOfWork.Repository<BookEdition, int>().DeleteRangeAsync(ids);
//
//             // Save to DB
//             if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
//             {
//                 var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
//                 return new ServiceResult(ResultCodeConst.SYS_Success0008,
//                     StringUtils.Format(msg, bookEditionList.Count.ToString()), true);
//             }
//
//             // Fail to delete
//             return new ServiceResult(ResultCodeConst.SYS_Fail0004,
//                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
//         }
//         catch (DbUpdateException ex)
//         {
//             if (ex.InnerException is SqlException sqlEx)
//             {
//                 switch (sqlEx.Number)
//                 {
//                     case 547: // Foreign key constraint violation
//                         return new ServiceResult(ResultCodeConst.SYS_Fail0007,
//                             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
//                 }
//             }
//
//             // Throw if other issues
//             throw;
//         }
//         catch (Exception ex)
//         {
//             _logger.Error(ex.Message);
//             throw new Exception("Error invoke when process delete range library item");
//         }
//     }
//
//     private List<LibraryItemDto> HandleEnumTextForEditions(List<LibraryItemDto> dtos)
//     {
// 	    // Determine current system lang
// 	    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
// 		    LanguageContext.CurrentLanguage);
// 	    var isEng = lang == SystemLanguage.English;
//
// 	    // BookEditionCopyStatus enum descriptions
// 	    var editionStatusDescriptions = Enum.GetValues(typeof(LibraryItemInstanceStatus))
// 		    .Cast<LibraryItemInstanceStatus>()
// 		    .ToDictionary(
// 			    e => e.ToString(),
// 			    e => EnumExtensions.GetDescription(e)
// 		    );
//
// 	    // BookFormat enum descriptions
// 	    var formatDescriptions = Enum.GetValues(typeof(BookFormat))
// 		    .Cast<BookFormat>()
// 		    .ToDictionary(
// 			    e => e.ToString(),
// 			    e => EnumExtensions.GetDescription(e)
// 		    );
//
// 	    if (dtos.Any())
// 	    {
// 		    foreach (var edition in dtos)
// 		    {
// 			    if (edition.BookEditionCopies.Any())
// 			    {
// 				    foreach (var copy in edition.BookEditionCopies)
// 				    {
// 					    if (!string.IsNullOrEmpty(copy.Status) &&
// 					        editionStatusDescriptions.TryGetValue(copy.Status, out var statusDesc))
// 					    {
// 						    copy.Status = isEng ? StringUtils.AddWhitespaceToString(copy.Status) : statusDesc;
// 					    }
// 				    }
// 			    }
//
// 			    if (!string.IsNullOrEmpty(edition.Format) &&
// 			        formatDescriptions.TryGetValue(edition.Format, out var formatDesc))
// 			    {
// 				    edition.Format = isEng ? edition.Format : formatDesc;
// 			    }
// 		    }
// 	    }
//
// 	    return dtos;
//     }
//
//     public async Task<IServiceResult> ImportAsync(
// 	    IFormFile? file, 
// 	    List<IFormFile> coverImageFiles,
// 	    string[]? scanningFields)
//     {
// 	    try
// 	    {
// 		    // Determine system lang
// 		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
// 			    .CurrentLanguage);
//
// 		    // Check exist file
// 		    if (file == null || file.Length == 0)
// 		    {
// 			    return new ServiceResult(ResultCodeConst.File_Warning0002,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
// 		    }
//
// 		    // Validate import file 
// 		    var validationResult = await ValidatorExtensions.ValidateAsync(file);
// 		    if (validationResult != null && !validationResult.IsValid)
// 		    {
// 			    // Response the uploaded file is not supported
// 			    throw new NotSupportedException(await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
// 		    }
//
// 		    // Csv config
// 		    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
// 		    {
// 			    HasHeaderRecord = true,
// 			    HeaderValidated = null,
// 			    MissingFieldFound = null
// 		    };
//
// 		    // Process read csv file
// 		    var readResp =
// 			    CsvUtils.ReadCsvOrExcelWithErrors<LibraryItemCsvRecord>(file, csvConfig, null, lang);
// 			if(readResp.Errors.Any())
// 			{
// 				var errorResps = readResp.Errors.Select(x => new ImportErrorResultDto()
// 				{	
// 					RowNumber = x.Key,
// 					Errors = x.Value.ToList()
// 				});
// 			    
// 				return new ServiceResult(ResultCodeConst.SYS_Fail0008,
// 					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
// 			}
// 		    
// 		    // Extract all cover image file name
// 		    var imageFileNames = coverImageFiles.Select(f => f.FileName).ToList();
// 		    // Find duplicate image file names
// 		    var duplicateFileNames = imageFileNames
// 			    .GroupBy(name => name)
// 			    .Where(group => group.Count() > 1) // Filter groups with more than one occurrence
// 			    .Select(group => group.Key)       // Select the duplicate file names
// 			    .ToList();
// 		    if (duplicateFileNames.Any())
// 		    {
// 			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0004);
// 			    
// 			    // Add single quotes to each file name
// 			    var formattedFileNames = duplicateFileNames
// 				    .Select(fileName => $"'{fileName}'"); 
//
// 			    return new ServiceResult(
// 				    ResultCodeConst.File_Warning0004,
// 				    StringUtils.Format(errMsg, String.Join(", ", formattedFileNames))
// 			    );
// 		    }
// 			
// 		    // Detect record errors
// 		    var detectResult =
// 			    await DetectWrongDataAsync(readResp.Records, imageFileNames, scanningFields, (SystemLanguage)lang!);
// 		    if (detectResult.Any())
// 		    {
// 			    var errorResps = detectResult.Select(x => new ImportErrorResultDto()
// 			    {	
// 					RowNumber = x.Key,
// 					Errors = x.Value
// 			    });
// 			    
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0008,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
// 		    }
//
// 		    // Handle upload images (Image name | URL)
// 		    var uploadFailList = new List<string>();
// 		    var imageUrlDic = new Dictionary<string, string>();
// 		    foreach (var coverImage in coverImageFiles)
// 		    {
// 			    // Try to validate file
// 			    var validateResult = await 
// 				    new ImageTypeValidator(lang.ToString() ?? SystemLanguage.English.ToString()).ValidateAsync(coverImage);
// 			    if (!validateResult.IsValid)
// 			    {
// 				    var isEng = lang == SystemLanguage.English;
// 				    return new ServiceResult(ResultCodeConst.SYS_Warning0001, isEng 
// 					    ? $"File '{coverImage.FileName}' is not a image file " +
// 					      $"Valid format such as (.jpeg, .png, .gif, etc.)" 
// 					    : $"File '{coverImage.FileName}' không phải là file hình ảnh. " +
// 					      $"Các loại hình ảnh được phép là: (.jpeg, .png, .gif, v.v.)");
// 			    }
// 			    
// 			    // Upload image to cloudinary
// 			    var uploadResult = (await _cloudService.UploadAsync(coverImage, FileType.Image, ResourceType.BookImage))
// 				    .Data as CloudinaryResultDto;
// 			    if (uploadResult == null)
// 			    {
// 				    // Add image that fail to upload
// 				    uploadFailList.Add(coverImage.FileName);
// 			    }
// 			    else
// 			    {
// 				    // Add to dic
// 				    imageUrlDic.Add(coverImage.FileName, uploadResult.SecureUrl);
// 			    }
// 		    }
//
// 		    var totalImported = 0;
// 		    var totalFailed = 0;
// 		    // Process import book editions
// 		    var successRecords = readResp.Records
// 			    .Where(r => !uploadFailList.Contains(r.CoverImage))
// 			    .ToList();
// 		    var failRecords = new List<LibraryItemCsvRecord>();
// 		    if (successRecords.Any())
// 		    {
// 			    // Group all editions have the same book code
// 			    var groupedRecords = successRecords
// 				    .GroupBy(r => r.BookCode)
// 				    .Select(e => new
// 				    {
// 					    Key = e.Key,
// 						Values = e.ToList()
// 				    });
// 			    
// 			    foreach (var record in groupedRecords)
// 			    {
// 				    // Initialize list editions
// 				    var editionList = new List<LibraryItemDto>();
// 				    foreach (var bookEdition in record.Values)
// 				    {
// 					    // Extract all edition copy barcodes
// 					    var editionCopyBarcodes = !string.IsNullOrWhiteSpace(bookEdition.EditionCopyBarcodes)
// 						    ? bookEdition.EditionCopyBarcodes.Split("\n").Select(barcode => new LibraryItemInstanceDto()
// 						    {
// 							    Barcode = barcode,
// 							    IsDeleted = false,
// 							    Status = nameof(LibraryItemInstanceStatus.OutOfShelf)
// 						    }).ToList()
// 						    : new List<LibraryItemInstanceDto>();
// 					    
// 					    // Extract all edition authors
// 					    var editionAuthorCodes = bookEdition.AuthorCodes.Split("\n").ToList();
// 					    // Get all author by code
// 					    var authorDtos = (await _authorService.GetAllByCodesAsync(editionAuthorCodes.ToArray())).Data as List<AuthorDto>;
// 						
// 					    // Get shelf location
// 					    var shelfDto = 
// 						    (await _libShelfService.GetWithSpecAsync(new BaseSpecification<LibraryShelf>(
// 						    s => s.ShelfNumber.ToLower() == bookEdition.ShelfNumber.ToLower()))
// 						    ).Data as LibraryShelfDto;
// 					    
// 					    // Add new edition
// 					    editionList.Add(new LibraryItemDto()
// 					    {
// 						    // Cover image
// 						    CoverImage = imageUrlDic.TryGetValue(bookEdition.CoverImage, out var coverImageUrl) ? coverImageUrl : null,
// 						    // Edition number
// 						    EditionNumber = bookEdition.EditionNumber,
// 						    // ISBN
// 						    Isbn = bookEdition.Isbn,
// 						    // Edition title
// 						    EditionTitle = bookEdition.EditionTitle,
// 						    // Summary
// 						    EditionSummary = bookEdition.Summary,
// 						    // Publication year
// 						    PublicationYear = bookEdition.PublicationYear,
// 						    // Page count
// 						    PageCount = bookEdition.PageCount,
// 						    // Language
// 						    Language = bookEdition.Language,
// 						    // Format
// 						    Format = bookEdition.Format,
// 						    // Publisher
// 						    Publisher = bookEdition.Publisher,
// 						    // Estimated price
// 						    EstimatedPrice = bookEdition.EstimatedPrice,
// 						    // Edition copies
// 						    BookEditionCopies = editionCopyBarcodes.ToList(),
// 						    // Authors
// 						    BookEditionAuthors = authorDtos != null && authorDtos.Any() 
// 							    ? authorDtos.Select(a => new LibraryItemAuthorDto()
// 								    {
// 									    AuthorId = a.AuthorId
// 								    }).ToList()
// 							    : null!,
// 						    // Library shelf 
// 						    ShelfId = shelfDto?.ShelfId,
// 						    // Book edition inventory
// 						    BookEditionInventory = new()
// 						    {
// 							    TotalCopies = editionCopyBarcodes.Count, // Count total copies
// 							    AvailableCopies = 0,
// 							    BorrowedCopies = 0,
// 							    RequestCopies = 0,
// 							    ReservedCopies = 0
// 						    },
// 						    
// 						    // Default values
// 						    IsTrained = false,
// 						    IsDeleted = false,
// 						    CanBorrow = false,
// 						    Status = LibraryItemStatus.Draft,
// 					    });
// 				    }
//
// 				    if (editionList.Any())
// 				    {
// 					    // Check for book code existence in DB
// 					    if ((await _bookService.Value.GetWithSpecAsync(new BaseSpecification<Book>(
// 						        b => b.BookCode.ToLower() == record.Key.ToLower()))).Data is BookDto bookDto) // already exist
// 					    {
// 						    // Add book id
// 						    editionList.ForEach(e => e.BookId = bookDto.BookId);
// 						    // Add range editions
// 						    await _unitOfWork.Repository<BookEdition, int>().AddRangeAsync(_mapper.Map<List<BookEdition>>(editionList));
// 					    }
// 					    else // not exist yet
// 					    {
// 						    // Extract all categories of first edition 
// 						    var editionCategoryNames = record.Values.First().Categories
// 							    .Split(", ")
// 							    .Select(x => x.Trim())
// 							    .ToList();
// 						    // Get all categories by name
// 						    var categoryDtos = 
// 							    (await _cateService.GetAllWithSpecAsync(new BaseSpecification<Category>(
// 							    c => editionCategoryNames.Contains(c.EnglishName)))
// 							    ).Data as IEnumerable<CategoryDto>;
// 						    // Process add new editions with book
// 						    bookDto = new BookDto()
// 						    {
// 							    // Title
// 							    Title = editionList.First().EditionTitle ?? string.Empty,
// 							    // Summary
// 							    Summary = editionList.First().EditionSummary,
// 							    // Book code
// 							    BookCode = record.Key,
// 							    // Book editions
// 							    BookEditions = editionList,
// 							    // Book categories
// 							    BookCategories = categoryDtos?.Select(cate => new BookCategoryDto()
// 							    {
// 								    CategoryId = cate.CategoryId
// 							    }).ToList() ?? null!,
// 							    
// 							    // Default value
// 							    IsDeleted = false,
// 							    BookCodeForAITraining = Guid.NewGuid()
// 						    };
// 						    
// 						    // Add new book
// 						    await _unitOfWork.Repository<Book, int>().AddAsync(_mapper.Map<Book>(bookDto));
// 					    }
// 					    
// 					    // Save change to DB
// 					    if(await _unitOfWork.SaveChangesAsync() > 0) totalImported = editionList.Count;
// 					    else failRecords.AddRange(record.Values);
// 				    }
// 			    }
// 		    }
// 		    
// 		    // Aggregate all book editions fail to upload & fail to save DB (if any)
// 		    failRecords.AddRange(readResp.Records
// 			    .Where(r => uploadFailList.Contains(r.CoverImage))
// 			    .ToList());
// 		    if (failRecords.Any()) totalFailed = failRecords.Count;
// 			
// 		    string message;
// 		    byte[]? fileBytes;
// 			// Generate a message based on the import and failure counts
// 		    if (totalImported > 0 && totalFailed == 0)
// 		    {
// 			    // All records imported successfully
// 			    message = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005), totalImported.ToString());
// 			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message);
// 		    }
//
// 		    if (totalImported > 0 && totalFailed > 0)
// 		    {
// 			    // Partial success with some failures
// 			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadBooks");
//
// 			    var baseMessage = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
// 			    var failMessage = lang == SystemLanguage.English
// 				    ? $", {totalFailed} failed to import"
// 				    : $", {totalFailed} thêm mới thất bại";
//
// 			    message = StringUtils.Format(baseMessage, totalImported.ToString()) + failMessage;
// 			    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
// 		    }
//
// 		    if (totalImported == 0 && totalFailed > 0)
// 		    {
// 			    // Complete failure
// 			    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadBooks");
// 			    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message, Convert.ToBase64String(fileBytes));
// 		    }
//
// 			// Default case: No records imported or failed
// 		    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
// 		    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message);
// 	    }
// 	    catch (UnprocessableEntityException)
// 	    {
// 		    throw;
// 	    }
// 	    catch (Exception ex)
// 	    {
// 		    _logger.Error(ex.Message);
// 		    throw new Exception("Error invoke when process import book editions");
// 	    }
//     }
//
//     public async Task<IServiceResult> ExportAsync(ISpecification<BookEdition> spec)
//     {
// 	    try
// 	    {
// 		    // Try to parse specification to BookEditionSpecification
// 		    var bookSpec = spec as LibraryItemSpecification;
// 		    // Check if specification is null
// 		    if (bookSpec == null)
// 		    {
// 			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
// 		    }				
// 			
// 		    // Apply include
// 		    bookSpec.ApplyInclude(q => q
// 			    .Include(be => be.Shelf)
// 			    .Include(be => be.Book)
// 				    .ThenInclude(b => b.BookCategories)
// 						.ThenInclude(bc => bc.Category)
// 			    .Include(be => be.BookEditionAuthors)
// 					.ThenInclude(bea => bea.Author)
// 			    .Include(be => be.BookEditionCopies)
// 		    );
// 		    // Get all with spec
// 		    var entities = await _unitOfWork.Repository<BookEdition, int>()
// 			    .GetAllWithSpecAsync(bookSpec, tracked: false);
// 		    if (entities.Any()) // Exist data
// 		    {
// 			    // Map entities to dtos 
// 			    var bookEditionDtos = _mapper.Map<List<LibraryItemDto>>(entities);
// 			    // Process export data to file
// 			    var fileBytes = CsvUtils.ExportToExcel(
// 				    bookEditionDtos.ToBookEditionCsvRecords());
//
// 			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
// 				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
// 				    fileBytes);
// 		    }
// 			
// 		    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
// 			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
// 	    }
// 	    catch (UnprocessableEntityException)
// 	    {
// 		    throw;
// 	    }
// 	    catch (Exception ex)
// 	    {
// 		    _logger.Error(ex.Message);
// 		    throw new Exception("Error invoke when process export book editions");
// 	    }
//     }
//     
//     private async Task<Dictionary<int, List<string>>> DetectWrongDataAsync(
// 	    List<LibraryItemCsvRecord> records,
// 	    List<string> coverImageNames,
// 	    string[]? scanningFields,
// 	    SystemLanguage lang)
// 	{
// 	    // Check system lang
// 	    var isEng = lang == SystemLanguage.English;
//
// 	    // Initialize dictionary to hold errors
// 	    var errorMessages = new Dictionary<int, List<string>>();
// 	    // Initialize hashset to check uniqueness
// 	    var coverImageSet = new HashSet<string>();
// 	    var editionNumbers = new HashSet<int>();
// 	    var editionCopyBarcodes = new HashSet<string>();
// 	    var editionCategories = new Dictionary<string, string[]>();
// 	    // Default row index set to second row, as first row is header
// 	    var currDataRow = 2;
//
// 	    foreach (var record in records)
// 	    {
// 	        // Initialize error list for the current row
// 	        var rowErrors = new List<string>();
//
// 	        // Validate edition number uniqueness
// 	        if (!editionNumbers.Add(record.EditionNumber))
// 	        {
// 	            rowErrors.Add(isEng ? "Edition number must be unique" : "Số thứ thự tài liệu không được trùng");
// 	        }
//
// 	        // Validate existing ISBN
// 	        var isExistIsbn = await _unitOfWork.Repository<BookEdition, int>()
// 	            .AnyAsync(be => be.Isbn == ISBN.CleanIsbn(record.Isbn));
// 	        if (isExistIsbn)
// 	        {
// 	            rowErrors.Add(isEng ? $"ISBN '{record.Isbn}' already exists" : $"Mã ISBN '{record.Isbn}' đã tồn tại");
// 	        }
//
// 	        // Validate existing cover image
// 	        if (!coverImageNames.Exists(str => str.Equals(record.CoverImage)))
// 	        {
// 	            rowErrors.Add(isEng ? $"Image file name '{record.CoverImage}' does not exist" : $"Không tìm thấy file hình có tên '{record.CoverImage}'");
// 	        }
//
// 	        // Validate shelf location
// 	        var isExistShelfLocation = (await _libShelfService.AnyAsync(x => x.ShelfNumber == record.ShelfNumber)).Data is true;
// 	        if (!isExistShelfLocation)
// 	        {
// 	            rowErrors.Add(isEng ? $"Shelf number '{record.ShelfNumber}' does not exist" : $"Kệ số '{record.ShelfNumber}' không tồn tại");
// 	        }
//
// 	        // Validate author codes
// 	        var authorCodes = record.AuthorCodes.Split("\n");
// 	        if (authorCodes.Length < 1)
// 	        {
// 	            rowErrors.Add(isEng ? "Please add at least one author" : "Vui lòng thêm tác giả");
// 	        }
// 	        else
// 	        {
// 	            var duplicateCodes = authorCodes.GroupBy(x => x)
// 	                .Where(g => g.Count() > 1)
// 	                .Select(g => g.Key)
// 	                .ToList();
//
// 	            if (duplicateCodes.Any())
// 	            {
// 	                rowErrors.Add(isEng ? $"The following author codes are duplicated: {string.Join(", ", duplicateCodes)}" : $"Các mã tác giả sau đây bị trùng lặp: {string.Join(", ", duplicateCodes)}");
// 	            }
//
// 	            foreach (var authCode in authorCodes)
// 	            {
// 	                var isExist = (await _authorService.AnyAsync(x => x.AuthorCode != null 
// 	                                                                && x.AuthorCode.ToLower() == authCode.ToLower())).Data is true;
// 	                if (!isExist)
// 	                {
// 	                    rowErrors.Add(isEng ? $"Author code '{authCode}' does not exist" : $"Mã tác giả '{authCode}' không tồn tại");
// 	                }
// 	            }
// 	        }
// 			
// 		    // Check exist copy codes
// 		    var editionCopyLength = record.EditionCopyBarcodes != null ? record.EditionCopyBarcodes.Split("\n").Length : 0;
// 		    if (editionCopyLength > 0)
// 		    {
// 			    var copyCodes = record.EditionCopyBarcodes!.Split("\n");
// 			    var duplicateCodes = copyCodes.GroupBy(x => x)
// 				    .Where(g => g.Count() > 1)
// 				    .Select(g => g.Key)
// 				    .ToList();
//
// 			    // Check whether code is duplicate within a single cell
// 			    if (duplicateCodes.Any())
// 			    {
// 				   rowErrors.Add(isEng
// 						   ? $"The following barcodes are duplicated: {string.Join(", ", duplicateCodes)}"
// 						   : $"Các mã barcode sau đây bị trùng lặp: {string.Join(", ", duplicateCodes)}");
// 			    }
// 			    
// 			    // Check whether code is duplicate within all other cells
// 			    foreach (var code in copyCodes)
// 			    {
// 				    if (!editionCopyBarcodes.Add(code)) 
// 				    {
// 					    rowErrors.Add(isEng
// 						    ? $"Barcode '{code}' already exists in file"
// 						    : $"Barcode '{code}' bị trùng trong file");
// 				    }
// 			    }
// 			    
// 			    // Check whether barcode already exist in DB
// 			    foreach (var barcode in copyCodes)
// 			    {
// 				    var isExist = (await _editionCopyService.Value.AnyAsync(x => x.Barcode.ToLower() == barcode.ToLower())).Data is true;
// 				    if (isExist)
// 				    {
// 					    rowErrors.Add(isEng
// 						    ? $"Barcode '{barcode}' already not exist"
// 						    : $"Barcode '{barcode}' đã tồn tại");
// 				    }
// 			    }
// 		    }
// 		    
// 		    // Check exist categories
// 		    if (record.Categories.Split(",").Length < 1)
// 	        {
// 		        rowErrors.Add(isEng
// 			        ? "Please add at least one category"
// 			        : "Vui lòng thêm thể loại");
// 	        }
// 		    else
// 		    {
// 			    var categoryNames = record.Categories.Split(",").Select(x => x.Trim()).ToList();
// 			    var duplicateNames = categoryNames.GroupBy(x => x)
// 				    .Where(g => g.Count() > 1)
// 				    .Select(g => g.Key)
// 				    .ToList();
//
// 			    if (duplicateNames.Any())
// 			    {
// 				    rowErrors.Add(isEng
// 					    ? $"The following categories are duplicated: {string.Join(", ", duplicateNames)}"
// 					    : $"Các thể loại sau đây bị trùng lặp: {string.Join(", ", duplicateNames)}");
// 			    }
//
// 			    foreach (var cateName in categoryNames)
// 			    {
// 				    var isExist = (await _cateService.AnyAsync(x => x.EnglishName == cateName)).Data is true;
// 				    if (!isExist)
// 				    {
// 					    rowErrors.Add(isEng
// 						    ? $"Category '{cateName}' does not exist"
// 						    : $"Thể loại '{cateName}' không tồn tại");
// 				    }
// 			    }
//
// 			    // Check whether exist different categories in the same edition
// 			    if (editionCategories.TryGetValue(record.BookCode, out var defaultCategoryNames)) // Exist book code value
// 			    {
// 				    // Check whether having different categories (ignoring order)
// 				    if (!defaultCategoryNames.OrderBy(x => x).SequenceEqual(categoryNames.OrderBy(x => x)))
// 				    {
// 					    rowErrors.Add(isEng
// 						    ? "Book categories must be shared the same within different editions in a single book"
// 						    : "Các thể loại sách phải giống nhau trong các tài liệu khác nhau trong một cuốn sách");
// 				    }
// 			    }
// 			    else // Not exist yet
// 			    {
// 				    // Add list category name as default to compare with others
// 				    editionCategories.Add(record.BookCode, categoryNames.ToArray());
// 			    }
// 		    }
// 		    
// 			// Check duplicate ISBN
// 			var isbnList = records.Select(x => x.Isbn).ToList();
// 			var isIsbnDuplicate = isbnList.Count(x => x == record.Isbn) > 1;
// 			if (isIsbnDuplicate)
// 			{
// 				rowErrors.Add(isEng ? $"IBSN '{record.Isbn}' is duplicated" : $"Mã ISBN '{record.Isbn}' bị trùng");
// 			}
// 		    
// 			// Check duplicate edition number
// 			// Not allow to have same edition with existing edition
// 			var isDuplicateEditionNum = await _unitOfWork.Repository<BookEdition, int>().AnyAsync(be =>
// 				be.Book.BookCode == record.BookCode && // Whether in same book
// 				be.EditionNumber == record.EditionNumber); // Check for specific edition		
// 			if (isDuplicateEditionNum)
// 			{
// 				rowErrors.Add(isEng 
// 					? $"Edition number '{record.EditionNumber}' already exists in database with book code '{record.BookCode}'" 
// 					: $"Số thự tự tài liệu '{record.EditionNumber}' đã tồn tại trong cơ sở dữ liệu với book code '{record.BookCode}'");
// 			}
// 			
// 		    // Validations
// 		    // TODO: Validate book code
// 		    if (record.EditionTitle.Length > 150) // Edition title
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Book edition title must not exceed than 150 characters"
// 				    : "Tiêu đề của tài liệu phải nhỏ hơn 150 ký tự");
// 		    }
// 		    if (record.Summary.Length > 500) // Edition summary
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Edition summary must not exceed 500 characters"
// 				    : "Mô tả của tài liệu không vượt quá 500 ký tự");
// 		    }
// 		    if (record.EditionNumber <= 0 && record.PageCount < int.MaxValue) // Edition number
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Book edition number is not valid"
// 				    : "Số thứ tự tài liệu không hợp lệ");
// 		    }
// 		    if (StringUtils.IsNumeric(record.Language) ||
// 		        StringUtils.IsDateTime(record.Language)) // Language
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Language is not valid"
// 				    : "Ngôn ngữ không hợp lệ");
// 		    }
// 		    if (StringUtils.IsNumeric(record.Format) || StringUtils.IsDateTime(record.Format)
// 		        || !Enum.TryParse(typeof(BookFormat), record.Format, true, out _)) // Format
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Book format is not valid"
// 				    : "Format sách không hợp lệ");
// 		    }
// 		    if (record.PageCount <= 0 && record.PageCount < int.MaxValue) // Page count
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Page count is not valid"
// 				    : "Tổng số trang không hợp lệ");
// 		    }
// 		    if (!(int.TryParse(record.PublicationYear.ToString(), out var year) 
// 		        && year > 0 && year <= DateTime.Now.Year)) // Publication year
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Publication year is not valid"
// 				    : "Năm xuất bản không hợp lệ");
// 		    }
// 		    if (StringUtils.IsNumeric(record.Publisher) || 
// 		        StringUtils.IsDateTime(record.Publisher)) // Publisher
// 		    {
// 			    rowErrors.Add(isEng
// 				    ? "Publisher is not valid"
// 				    : "Tên nhà xuất bản không hợp lệ");
// 		    }
//
// 		    if (record.EstimatedPrice < 1000 || record.EstimatedPrice > 9999999999)
// 		    {
// 			    if (record.EstimatedPrice < 1000)
// 			    {
// 				    rowErrors.Add(isEng
//                         ? "EstimatedPrice must be at least 1.000 VND"
//                         : "Giá phải ít nhất là 1.000 VND");
// 			    }
// 			    else if (record.EstimatedPrice > 9999999999)
// 			    {
// 				    rowErrors.Add(isEng
// 					    ? "EstimatedPrice exceeds the maximum limit of 9.999.999.999 VND"
// 					    : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND");
// 			    }
// 		    }
// 		    if (!ISBN.IsValid(record.Isbn, out _)) // Isbn
// 		    {
// 			    rowErrors.Add(isEng
// 	                ? "ISBN is not valid"
// 	                : "Mã ISBN không hợp lệ");
// 		    }
// 		    
// 		    if (scanningFields != null)
// 		    {
// 			    // Initialize library item base spec
// 			    BaseSpecification<BookEdition>? editionBaseSpec = null;
// 			    // Initialize book base spec
// 			    BaseSpecification<Book>? bookBaseSpec = null;
// 			    // Initialize duplicate field
// 			    var isImageDuplicate = false;
// 			    
// 			    // Iterate each fields to add criteria scanning logic
// 			    foreach (var field in scanningFields)
// 			    {
// 				    var normalizedField = field.ToUpperInvariant();
// 				    
// 				    // Building query to check duplicates on BookEdition entity
// 				    var newEditionSpec = normalizedField switch
// 				    {
// 					    var editionTitle when editionTitle == nameof(BookEdition.Title).ToUpperInvariant() =>
// 						    new BaseSpecification<BookEdition>(e => e.Title != null && e.Title.Equals(record.EditionTitle)),
// 					    _ => null
// 				    };
// 				    
// 				    // Build query to check duplicates on Book entity
// 				    var newBookSpec = normalizedField switch
// 				    {
// 					    var bookCode when bookCode == nameof(Book.BookCode).ToUpperInvariant() =>
// 						    new BaseSpecification<Book>(b => b.BookCode.ToLower() == record.BookCode.ToLower()),
// 					    _ => null
// 				    };
// 				    
// 				    if (newEditionSpec != null) // Found new edition spec
// 				    {
// 					    // Combine specifications with AND logic
// 					    editionBaseSpec = editionBaseSpec == null
// 						    ? newEditionSpec
// 						    : editionBaseSpec.Or(newEditionSpec);
// 				    }
// 				    
// 				    if (newBookSpec != null) // Found new book spec
// 				    {
// 					    // Combine specifications with AND logic
// 					    bookBaseSpec = bookBaseSpec == null
// 						    ? newBookSpec
// 						    : bookBaseSpec.Or(newBookSpec);
// 				    }
// 				    
// 				    // Check whether existing scanning field (CoverImage) and cannot add to hashset due to duplicate
// 				    if (normalizedField == nameof(BookEdition.CoverImage).ToUpperInvariant()
// 				        && !coverImageSet.Add(record.CoverImage))
// 					{
// 						isImageDuplicate = true;
// 					}
// 			    }
// 			    
// 			    // Check exist with spec
// 			    if (editionBaseSpec != null && await _unitOfWork.Repository<BookEdition, int>().AnyAsync(editionBaseSpec))
// 			    {
// 				    rowErrors.Add(isEng ? $"Title '{record.EditionTitle}'" : $"Tên sách '{record.EditionTitle}' đã tồn tại");
// 			    }
// 			    if (bookBaseSpec != null && await _unitOfWork.Repository<Book, int>().AnyAsync(bookBaseSpec))
// 			    {
// 				    rowErrors.Add(isEng ? $"Book code '{record.BookCode}' already exists" : $"Mã sách '{record.BookCode}' đã tồn tại");
// 			    }
// 			    
// 			    // Check whether image is duplicate
// 			    if (isImageDuplicate)
// 			    {
// 				    rowErrors.Add(isEng ? 
// 					    $"Cover image '{record.CoverImage} is duplicated'" : 
// 					    $"Hình '{record.CoverImage}' bị trùng");
// 			    }
// 		    }
// 	        
// 	        // if errors exist for the row, add to the dictionary
// 	        if (rowErrors.Any())
// 	        {
// 	            errorMessages.Add(currDataRow, rowErrors);
// 	        }
//
// 	        // Increment the row counter
// 	        currDataRow++;
// 	    }
//
// 	    return errorMessages;
// 	}
}