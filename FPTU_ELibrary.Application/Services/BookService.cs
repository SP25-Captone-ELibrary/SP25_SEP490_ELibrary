using System.Text.Json;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;

namespace FPTU_ELibrary.Application.Services
{
	public class BookService : GenericService<Book, BookDto, int>, IBookService<BookDto>
	{
		private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;
		private readonly IEmployeeService<EmployeeDto> _empService;
		private readonly ICategoryService<CategoryDto> _cateService;
		private readonly IAuthorService<AuthorDto> _authorService;
		private readonly IBookCategoryService<BookCategoryDto> _bookCateService;
		private readonly ICloudinaryService _cloudService;
		private readonly IBookEditionCopyService<BookEditionCopyDto> _editionCopyService;

		public BookService(
			IAuthorService<AuthorDto> authorService,
			IBookCategoryService<BookCategoryDto> bookCateService,
			IBookEditionCopyService<BookEditionCopyDto> editionCopyService,
			ICategoryService<CategoryDto> cateService,
			IEmployeeService<EmployeeDto> empService,
			ILibraryShelfService<LibraryShelfDto> libShelfService,
			ICloudinaryService cloudService,
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			ILogger logger) 
			: base(msgService, unitOfWork, mapper, logger)
		{
			_libShelfService = libShelfService;
			_empService = empService;
			_bookCateService = bookCateService;
			_editionCopyService = editionCopyService;
			_cateService = cateService;
			_authorService = authorService;
			_cloudService = cloudService;
		}

		public override async Task<IServiceResult> GetByIdAsync(int id)
		{
			try
			{
				// Build specification
				var baseSpec = new BaseSpecification<Book>(b => b.BookId == id);
				var bookEntity = await _unitOfWork.Repository<Book, int>()
					.GetWithSpecAndSelectorAsync(baseSpec, s => new Book()
					{
						BookId = s.BookId,
						Title = s.Title,
						SubTitle = s.SubTitle,
						Summary = s.Summary,
						IsDeleted = s.IsDeleted,
						IsDraft = s.IsDraft,
						CreatedAt = s.CreatedAt,
						CreatedBy = s.CreatedBy,
						UpdatedAt = s.UpdatedAt,
						UpdatedBy = s.UpdatedBy,
						BookResources = s.BookResources,
						BookEditions = s.BookEditions.Select(be => new BookEdition()
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
                            BookEditionInventory = be.BookEditionInventory,
                            BookEditionCopies = be.BookEditionCopies,
                            BookReviews = be.BookReviews,
                            Shelf = be.Shelf,
                            BookEditionAuthors = be.BookEditionAuthors.Select(ba => new BookEditionAuthor()
                            {
	                            BookEditionAuthorId = ba.BookEditionAuthorId,
	                            BookEditionId = ba.BookEditionId,
	                            AuthorId = ba.AuthorId,
	                            Author = ba.Author
                            }).ToList(),
                        }).ToList(),
						BookCategories = s.BookCategories.Select(bc => new BookCategory()
						{
							BookCategoryId = bc.BookCategoryId,
							BookId = bc.BookId,
							CategoryId = bc.CategoryId,
							Category = bc.Category
						}).ToList()
					});

				if (bookEntity != null)
				{
					// Map to dto
					var dto = _mapper.Map<BookDto>(bookEntity);
					// Handle enum text
					dto = HandleEnumText(dto);
					// Convert to book detail dto
					var bookDetailDto = dto.ToBookDetailDto();
					
					return new ServiceResult(ResultCodeConst.SYS_Success0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), bookDetailDto);
				}

				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get book");
			}
		}

		public async Task<IServiceResult> UpdateAsync(int id, BookDto dto, string byEmail)
		{
			try
			{
				// Determine system lang
				var langEnum =
					(SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext.CurrentLanguage);
				var isEng = langEnum == SystemLanguage.English;
				
				// Validate inputs using the generic validator
                var validationResult = await ValidatorExtensions.ValidateAsync(dto);
                // Check for valid validations
                if (validationResult != null && !validationResult.IsValid)
                {
                	// Convert ValidationResult to ValidationProblemsDetails.Errors
                	var errors = validationResult.ToProblemDetails().Errors;

	                // Check if errors contains BookEditions validation (skip for update)
	                if (errors.TryGetValue("bookEditions", out _)) errors.Remove("bookEditions");
	                
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
                
                // Select list of category ids
                var cateIds = dto.BookCategories.Select(c => c.CategoryId).ToList();
                // Count total exist result
                var countResult = await _cateService.CountAsync(
	                new BaseSpecification<Category>(ct => cateIds.Contains(ct.CategoryId)));
                // Check exist any category not being counted
                if (int.TryParse(countResult.Data?.ToString(), out var total) // Parse result to integer
                    && total != cateIds.Count) // Not exist 1-many category
                {
	                return new ServiceResult(ResultCodeConst.Book_Warning0001,
		                await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0001));
                }
                
                // Build specification
                var baseSpec = new BaseSpecification<Book>(b => b.BookId == id);
                // Apply include
                baseSpec.ApplyInclude(q => q
	                .Include(b => b.BookCategories)
	                .ThenInclude(bc => bc.Category)
	                .Include(b => b.BookResources)
                );
                // Get to update book with specification
                var toUpdateBook = await _unitOfWork.Repository<Book, int>().GetWithSpecAsync(baseSpec);
                if (toUpdateBook == null)
                {
	                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
		                StringUtils.Format(errMsg, isEng ? "book to update" : "sách để sửa"));
                }
                
                // Select all current cate ids
                var currentCateIds = toUpdateBook.BookCategories.Select(c => c.CategoryId).ToList();
				// Select new categories
                var toAddCate = cateIds.Where(x => !currentCateIds.Contains(x)).ToList();
                // Select remove categories
                var toRemoveCate = currentCateIds.Where(x => !cateIds.Contains(x)).ToList();
                
                // Process add categories
                foreach (var cateId in toAddCate)
                {
	                toUpdateBook.BookCategories.Add(new BookCategory()
	                {
		                CategoryId = cateId
	                });
                }
                // Process remove categories
                foreach (var cateId in toRemoveCate)
                {
	                // Do not need to check exist, as it retrieve from database
	                var bc = toUpdateBook.BookCategories.First(bc => bc.CategoryId == cateId);
	                toUpdateBook.BookCategories.Remove(bc);
                }
                
                // Update other properties
                toUpdateBook.Title = dto.Title;
                toUpdateBook.SubTitle = dto.SubTitle;
                toUpdateBook.Summary = dto.Summary;
                
                // Progress update when all require passed
                await _unitOfWork.Repository<Book, int>().UpdateAsync(toUpdateBook);

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
				throw new Exception("Error invoke when process update book");
			}
		}
		
		public async Task<IServiceResult> CreateAsync(BookDto dto, string byEmail)
		{
			try
			{
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

				// Select list of category ids
				var cateIds = dto.BookCategories.Select(c => c.CategoryId).ToList();
				// Count total exist result
				var countCateResult = await _cateService.CountAsync(
					new BaseSpecification<Category>(ct => cateIds.Contains(ct.CategoryId)));
				// Check exist any category not being counted
				if (int.TryParse(countCateResult.Data?.ToString(), out var totalCate) // Parse result to integer
				    && totalCate != cateIds.Count) // Not exist 1-many category
				{
					return new ServiceResult(ResultCodeConst.Book_Warning0001,
						await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0001));
				}

				// Select list of author ids
				var authorIds = dto.BookEditions
					.SelectMany(c => c.BookEditionAuthors)
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
				
				// Check resources existence
				foreach (var br in dto.BookResources)
				{
					// Get file type
					Enum.TryParse(typeof(FileType), br.FileFormat, out var fileType);
					// Check exist resource
					var checkExistResult = await _cloudService.IsExistAsync(br.ProviderPublicId, (FileType)fileType!);
					if (checkExistResult.Data is false) // Return when not found resource on cloud
					{
						return new ServiceResult(ResultCodeConst.Cloud_Warning0003, 
							await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0003));
					} 
				}
				
				// Add boolean information
				dto.IsDeleted = false;
				dto.IsDraft = true;

				// Add book edition creation information (if any)
				// Initialize numeric collection to check edition unique num
				var editionNums = new HashSet<int>();
				var editionCopyCodes = new HashSet<string>();
				var listBookEdition = dto.BookEditions.ToList();
				for (int i = 0; i < listBookEdition.Count; i++)
				{
					var be = listBookEdition[i];

					// Check unique book edition number
					if (!editionNums.Add(be.EditionNumber))
					{
						// Add error 
						customErrors.Add(
							$"bookEditions[{i}].editionNumber", 
							[await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0003)]);
					};
					
					// Check exist cover image
					if (!string.IsNullOrEmpty(be.CoverImage))
					{
						// Initialize field
						var isImageOnCloud = true;
						
						// Extract provider public id
						var publicId = StringUtils.GetPublicIdFromUrl(be.CoverImage);
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
                    var listBookEditionCopy = be.BookEditionCopies.ToList();
                    for (int j = 0; j < listBookEditionCopy.Count; j++)
                    {
	                    var bec = listBookEditionCopy[j];
	                    
                        if (editionCopyCodes.Add(bec.Code!)) // Add to hash set string to ensure uniqueness
                        {
							// Check already exist code in DB
							var checkExistResult = await _editionCopyService.AnyAsync(
								c => c.Code == bec.Code);
							if (checkExistResult.Data is true)
							{
								var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0006);
								// Add error 
								customErrors.Add(
									$"bookEditions[{i}].bookCopies[{j}].code", 
									[StringUtils.Format(errMsg, $"'{bec.Code!}'")]);
							}
                        }
                        else // Duplicate found
                        {
	                        // Add error 
	                        customErrors.Add(
		                        $"bookEditions[{i}].bookCopies[{j}].code", 
		                        [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0005)]);
                        };
                        
                        // Default status
                        bec.Status = nameof(BookEditionCopyStatus.OutOfShelf);
                        // Boolean 
                        bec.IsDeleted = false;
                    }
                    
                    // Default value
                    be.IsDeleted = false;
                    be.CanBorrow = false;
                    // Clear ISBN hyphens
                    be.Isbn = ISBN.CleanIsbn(be.Isbn);
                    // Check exist Isbn
                    var isIsbnExist = await _unitOfWork.Repository<BookEdition, int>()
	                    .AnyAsync(x => x.Isbn == be.Isbn);
                    if (isIsbnExist) // already exist 
                    {
	                    // Add error
	                    customErrors.Add(
		                    $"bookEditions[{i}].isbn", 
		                    // Isbn already exist message
		                    [await _msgService.GetMessageAsync(ResultCodeConst.Book_Warning0007)]);
                    }
				}
				
				// Any errors invoke when checking valid data
				if (customErrors.Any()) // exist errors
				{
					throw new UnprocessableEntityException("Invalid data", customErrors);
				}
				
				// Process create new book
				await _unitOfWork.Repository<Book, int>().AddAsync(_mapper.Map<Book>(dto));
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
				throw new Exception("Error invoke when process create new book");
			}
		}

		public async Task<IServiceResult> GetBookEnumsAsync()
		{
			try
			{
				// Book formats
				var bookFormats = new List<string>()
				{
					nameof(BookFormat.Paperback),
					nameof(BookFormat.HardCover)
				};
				
				// Book resource types
				var bookResourceTypes = new List<string>()
				{
					nameof(BookResourceType.Ebook),
					nameof(BookResourceType.AudioBook)
				};
				
				// File formats
				var fileFormats = new List<string>()
				{
					nameof(FileType.Image),
					nameof(FileType.Video)
				};
				
				// Resource provider
				var resourceProviders = new List<string>()
				{
					nameof(ResourceProvider.Cloudinary)
				};
				
				// Book copy statuses
				var bookCopyStatuses = new List<string>()
				{
					nameof(BookEditionCopyStatus.InShelf),
					nameof(BookEditionCopyStatus.OutOfShelf),
					nameof(BookEditionCopyStatus.Borrowed),
					nameof(BookEditionCopyStatus.Reserved),
				};
				
				// Copy condition statuses
				var conditionStatuses = new List<string>()
				{
					nameof(ConditionHistoryStatus.Good),
					nameof(ConditionHistoryStatus.Worn),
					nameof(ConditionHistoryStatus.Damaged),
					nameof(ConditionHistoryStatus.Lost)
				};
				
				// Get all shelves
				var bookShelves = (await _libShelfService.GetAllAsync()).Data as List<LibraryShelfDto>;

				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					new
					{
						BookFormats = bookFormats,
						BookResourceTypes = bookResourceTypes,
						BookShelves = bookShelves,
						FileFormats = fileFormats,
						ResourceProviders = resourceProviders,
						BookCopyStatuses = bookCopyStatuses,
						ConditionStatuses = conditionStatuses
					});
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get create information");
			}
		}

		private BookDto HandleEnumText(BookDto dto)
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
         	
             if (dto.BookEditions.Any())
             {
                foreach (var edition in dto.BookEditions)
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
             
             return dto;
        }
	}
}
