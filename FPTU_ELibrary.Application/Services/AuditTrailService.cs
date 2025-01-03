using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AuditTrail;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Serilog;
using BookCategory = FPTU_ELibrary.Domain.Common.Enums.BookCategory;

namespace FPTU_ELibrary.Application.Services;

public class AuditTrailService : ReadOnlyService<AuditTrail, AuditTrailDto, int>,
    IAuditTrailService<AuditTrailDto>
{
    private readonly IBookService<BookDto> _bookService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly IUserService<UserDto> _userService;
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ICategoryService<CategoryDto> _cateService;

    public AuditTrailService(
        IUserService<UserDto> userService,
        IAuthorService<AuthorDto> authorService,
        ICategoryService<CategoryDto> cateService,
        IEmployeeService<EmployeeDto> employeeService,
        IBookService<BookDto> bookService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _authorService = authorService;
        _cateService = cateService;
        _userService = userService;
        _employeeService = employeeService;
        _bookService = bookService;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<AuditTrail> spec, bool tracked = true)
    {
        try
        {
            // Try to parse specification to AuditTrailSpecification
            var auditTrailSpec = spec as AuditTrailSpecification;
            // Check if specification is null
            if (auditTrailSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
            	    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }		
            
            // Count total audits
            var totalAuditWithSpec = await _unitOfWork.Repository<AuditTrail, int>().CountAsync(auditTrailSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalAuditWithSpec / auditTrailSpec.PageSize);
            
            // Set pagination to specification after count total audits 
            if (auditTrailSpec.PageIndex > totalPage 
                || auditTrailSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                auditTrailSpec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            auditTrailSpec.ApplyPaging(
                skip: auditTrailSpec.PageSize * (auditTrailSpec.PageIndex - 1), 
                take: auditTrailSpec.PageSize);
            
            // Add order
            auditTrailSpec.AddOrderBy(a => a.DateUtc);
            
            // Retrieve all audit trail with spec
            var auditEntities = await _unitOfWork.Repository<AuditTrail, int>()
                .GetAllWithSpecAsync(auditTrailSpec);
            
            if (auditEntities.Any()) // Exist data
            {
            	// Convert to dto collection 
                var auditDtos = _mapper.Map<List<AuditTrailDto>>(auditEntities);
                
            	// Pagination result 
            	var paginationResultDto = new PaginatedResultDto<AuditTrailDto>(auditDtos,
                    auditTrailSpec.PageIndex, auditTrailSpec.PageSize, totalPage, totalAuditWithSpec);
            	
            	// Response with pagination 
            	return new ServiceResult(ResultCodeConst.SYS_Success0002, 
            		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
            	await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
            	// Mapping entities to dto 
            	_mapper.Map<List<AuditTrailDto>>(auditEntities));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all audit trail by book id");
        }
    }
    
    public async Task<IServiceResult> GetAuditDetailByDateUtcAndEntityNameAsync(string dateUtc, string rootEntityName, TrailType trailType)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Format dateUtc to (yyyy-MM-dd:HH:mm:ss.fff)
            var reqDateTime = DateTime.Parse(dateUtc, null, DateTimeStyles.RoundtripKind);
            // Build spec
            var baseSpec = BuildAuditTrailSpecification(reqDateTime, rootEntityName, trailType);
            // Retrieve all audit trail by DateUTC
            var auditTrails = await _unitOfWork.Repository<AuditTrail, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Convert to list
            var auditTrailList = auditTrails.OrderBy(at => at.EntityName).ToList();
            
            // Check whether audit trail exist
            if (auditTrailList.All(x => x.TrailType == TrailType.Added)) // All audit is mark as added
            {
                // Json Type
                Object? oldData = null;
                // Json Type
                Object? newData = null;
                
                // Handle by root entity name
                switch (rootEntityName)
                {
                    case nameof(Book):
                        newData = (await HandleBookAuditAsync(auditTrailList))?.ToBookDetailDto();
                        break;
                    case nameof(BookEdition):
                        newData = (await HandleBookEditionAddedAsync(auditTrailList))
                            .Select(be => be.ToEditionDetailDto())
                            .ToList();
                        break;
                    case nameof(BookResource):
                        newData = await HandleBookResourceAddedAsync(auditTrailList);
                        break;
                    case nameof(BookCategory):
                        newData = await HandleBookCategoryAddedAsync(auditTrailList);
                        break;
                    case nameof(BookEditionCopy):
                        newData = await HandleBookEditionCopyAddedAsync(auditTrailList);
                        break;
                    case nameof(BookEditionAuthor):
                        newData = await HandleBookEditionAuthorAddedAsync(auditTrailList);
                        break;
                    case nameof(CopyConditionHistory):
                        break;
                    case nameof(SystemRole):
                        break;
                    case nameof(RolePermission):
                        break;
                }

                if (oldData == null && newData == null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
                }
                
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                    new AuditTrailDetailDto()
                    {
                        OldValue = oldData,
                        NewValue = newData
                    });
            }
            else if(auditTrailList.Any()) // TrailType include [Added]/[None]/[Modified]/[Deleted]
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), auditTrailList);
            }
            
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get book audit detail");
        }
    }

    private BaseSpecification<AuditTrail> BuildAuditTrailSpecification(DateTime dateUtc, string rootEntityName, TrailType trailType)
    {
        // Try to get all related entities of root entity
        var relatedEntities = GetAllRelatedEntities(rootEntityName);
        
        // Initialize default base specification, which not check for specific trail type
        // Use default for [None]/[Modified]/[Deleted]
        var baseSpec = new BaseSpecification<AuditTrail>(at =>
            at.DateUtc == dateUtc && // with specific DateUTC 
            // Do not include specific trail type, as other entities may mark as deleted, modified or added
            relatedEntities.Contains(at.EntityName)); // match any of the related entities
        
        switch (trailType)
        {
            case TrailType.None:
                break;
            case TrailType.Added:
                baseSpec = new BaseSpecification<AuditTrail>(at =>
                    at.DateUtc == dateUtc && // with specific DateUTC 
                    at.TrailType == trailType && // with specific trail type when added
                    relatedEntities.Contains(at.EntityName)); // match any of the related entities
                break;
            case TrailType.Modified:
                break;
            case TrailType.Deleted:
                break;
        }

        return baseSpec;
    }
    
    
    #region Handle get entity relation columns (using recursive method) 
    private static readonly Dictionary<string, List<string>> EntityRelationships = new()
    {
        { nameof(Book), new List<string> { nameof(BookCategory), nameof(BookEdition), nameof(BookResource) } },
        { nameof(BookEdition) , new List<string> { nameof(BookEditionAuthor), nameof(BookEditionCopy) } },
        { nameof(BookEditionCopy), new List<string> { nameof(CopyConditionHistory) } }
    };

    private void ExpandRelatedEntities(string entityName, HashSet<string> result)
    {
        if (!EntityRelationships.ContainsKey(entityName))
            return;

        foreach (var relatedEntity in EntityRelationships[entityName])
        {
            if (result.Add(relatedEntity)) // Add to the result set if not already present
            {
                ExpandRelatedEntities(relatedEntity, result); // Recursively expand
            }
        }
    }
    
    private IEnumerable<string> GetAllRelatedEntities(string entityName)
    {
        var result = new HashSet<string> { entityName };
        ExpandRelatedEntities(entityName, result);
        return result;
    }
    #endregion

    #region Handle Added Entities
    private async Task<BookDto?> HandleBookAuditAsync(List<AuditTrail> auditTrailList)
    {
        var bookDto = auditTrailList // From audit trail collection
            .Where(b => b.EntityName == nameof(Book)) // Retrieve Book entity
            .Select(b => new BookDto() 
            {
                BookId = int.Parse(b.EntityId ?? "0"),
                Title = b.NewValues["Title"]?.ToString()!,
                SubTitle = b.NewValues["SubTitle"]?.ToString(),
                Summary = b.NewValues["Summary"]?.ToString(),
                IsDeleted = bool.Parse(b.NewValues["IsDeleted"]?.ToString()!),
                CreatedAt = DateTime.Parse(b.NewValues["CreatedAt"]?.ToString()!),
                UpdatedAt = DateTime.Parse(b.NewValues["UpdatedAt"]?.ToString() ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                CreatedBy = b.NewValues["CreatedBy"]?.ToString()!,
                UpdatedBy = b.NewValues["UpdatedBy"]?.ToString() ?? null!
            }).FirstOrDefault();
        // Check whether exist book
        if (bookDto == null) return null;
        
        bookDto.BookEditions = await HandleBookEditionAddedAsync(auditTrailList, bookDto.BookId.ToString());
        // Retrieve book categories (if any)
        bookDto.BookCategories = await HandleBookCategoryAddedAsync(auditTrailList, bookDto.BookId.ToString());
        // Retrieve book resources (if any)
        bookDto.BookResources = await HandleBookResourceAddedAsync(auditTrailList, bookDto.BookId.ToString());

        return bookDto;
    }
    
    private async Task<List<BookCategoryDto>> HandleBookCategoryAddedAsync(List<AuditTrail> auditTrailList, string? bookId = null)
    {
        var bookCategoryAudits = auditTrailList // From audit trail collection
                    .Where(bc => bc.EntityName == nameof(BookCategory)) // Retrieve all BookCategory entity 
                    .ToList(); // Convert to list
        if (bookCategoryAudits.Any()) // Exist at least 1 category 
        {
            // Handle map audit values to BookCategory collection
            var bookCategoryDtos = auditTrailList
                .Where(bc => bc.EntityName == nameof(BookCategory) && 
                             bc.NewValues.Any() &&
                             bc.NewValues.ContainsKey("BookId") &&
                             (string.IsNullOrEmpty(bookId) || bc.NewValues["BookId"]?.ToString() == bookId))
                .Select(bc => new BookCategoryDto()
                {
                    BookCategoryId = int.Parse(bc.EntityId ?? "0"),
                    BookId = int.Parse(bc.NewValues["BookId"]?.ToString() ?? "0"),
                    CategoryId = int.Parse(bc.NewValues["CategoryId"]?.ToString()!),
                    CreatedAt = DateTime.Parse(bc.NewValues["CreatedAt"]?.ToString()!),
                    UpdatedAt = DateTime.Parse(bc.NewValues["UpdatedAt"]?.ToString() ??
                                               DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    CreatedBy = bc.NewValues["CreatedBy"]?.ToString()!,
                    UpdatedBy = bc.NewValues["UpdatedBy"]?.ToString() ?? null!,
                }).ToList();
            
            // Try to retrieve category
            if (bookCategoryDtos.Any())
            {
                foreach (var bookCate in bookCategoryDtos)
                {
                    bookCate.Category = (await _cateService.GetByIdAsync(bookCate.CategoryId)).Data as CategoryDto ?? null!;
                }
            }

            return bookCategoryDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<BookCategoryDto>();
    }

    private async Task<List<BookResourceDto>> HandleBookResourceAddedAsync(List<AuditTrail> auditTrailList, string? bookId = null)
    {
        var bookResourceAudits = auditTrailList // From audit trail collection
            .Where(br => br.EntityName == nameof(BookResource)) // Retrieve all BookResource entity 
            .ToList(); // Convert to list  
        if (bookResourceAudits.Any()) // Exist at least 1 resource 
        {
            return await Task.FromResult(
                auditTrailList
                    .Where(br => br.EntityName == "BookResource" && 
                                 br.NewValues.Any() &&
                                 br.NewValues.ContainsKey("BookId") &&
                                 (string.IsNullOrEmpty(bookId) || br.NewValues["BookId"]?.ToString() == bookId))
                    .Select(br => new BookResourceDto
                    {
                        ResourceId = int.Parse(br.EntityId ?? "0"),
                        BookId = int.Parse(br.NewValues["BookId"]?.ToString() ?? "0"),
                        ResourceType = br.NewValues["ResourceType"]?.ToString()!,
                        ResourceUrl = br.NewValues["ResourceUrl"]?.ToString()!,
                        ResourceSize = int.Parse(br.NewValues["ResourceSize"]?.ToString() ?? "0"),
                        FileFormat = br.NewValues["FileFormat"]?.ToString()!,
                        Provider = br.NewValues["Provider"]?.ToString()!,
                        ProviderPublicId = br.NewValues["ProviderPublicId"]?.ToString()!,
                        ProviderMetadata = br.NewValues["ProviderMetadata"]?.ToString(),
                        CreatedAt = DateTime.Parse(br.NewValues["CreatedAt"]?.ToString()!),
                        UpdatedAt = DateTime.Parse(br.NewValues["UpdatedAt"]?.ToString() ??
                                                   DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                        CreatedBy = br.NewValues["CreatedBy"]?.ToString()!,
                        UpdatedBy = br.NewValues["UpdatedBy"]?.ToString() ?? null!,
                        IsDeleted = bool.Parse(br.NewValues["IsDeleted"]?.ToString()!),
                    }).ToList());
        }
        
        // Return empty collection <- Not found any
        return new List<BookResourceDto>();
    }

    private async Task<List<BookEditionDto>> HandleBookEditionAddedAsync(List<AuditTrail> auditTrailList, string? bookId = null)
    {
        var bookEditionAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(BookEdition)) // Retrieve all BookEdition entity 
            .ToList(); // Convert to list
        if (bookEditionAudits.Any()) // Exist at least 1 edition 
        {
            // Handle map audit values to BookEdition collection
            var bookEditionDtos = bookEditionAudits
                .Where(be => be.EntityName == nameof(BookEdition) && 
                             be.NewValues.Any() &&
                             be.NewValues.ContainsKey("BookId") &&
                             (string.IsNullOrEmpty(bookId) || be.NewValues["BookId"]?.ToString() == bookId))
                .Select(be => new BookEditionDto
                {
                    BookEditionId = int.Parse(be.EntityId ?? "0"),
                    BookId = int.Parse(be.NewValues["BookId"]?.ToString() ?? "0"),
                    EditionTitle = be.NewValues["EditionTitle"]?.ToString(),
                    EditionSummary = be.NewValues["EditionSummary"]?.ToString(),
                    EditionNumber = int.Parse(be.NewValues["EditionNumber"]?.ToString()!),
                    PublicationYear = int.Parse(be.NewValues["PublicationYear"]?.ToString()!),
                    PageCount = int.Parse(be.NewValues["PageCount"]?.ToString()!),
                    Language = be.NewValues["Language"]?.ToString()!,
                    CoverImage = be.NewValues["CoverImage"]?.ToString(),
                    Format = be.NewValues["Format"]?.ToString(),
                    Publisher = be.NewValues["Publisher"]?.ToString(),
                    Isbn = be.NewValues["Isbn"]?.ToString()!,
                    IsDeleted = bool.Parse(be.NewValues["IsDeleted"]?.ToString()!),
                    CanBorrow = bool.Parse(be.NewValues["CanBorrow"]?.ToString()!),
                    EstimatedPrice = decimal.Parse(be.NewValues["EstimatedPrice"]?.ToString()!),
                    ShelfId = int.Parse(be.NewValues["ShelfId"]?.ToString() ?? "0"),
                    Status = (BookEditionStatus) Enum.Parse(typeof(BookEditionStatus), be.NewValues["Status"]?.ToString()!),
                    CreatedAt = DateTime.Parse(be.NewValues["CreatedAt"]?.ToString()!),
                    UpdatedAt = DateTime.Parse(be.NewValues["UpdatedAt"]?.ToString() ??
                                               DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    CreatedBy = be.NewValues["CreatedBy"]?.ToString()!,
                    UpdatedBy = be.NewValues["UpdatedBy"]?.ToString() ?? null!,
                }).ToList();
            
            
            // Try to retrieve all authors & copies
            if (bookEditionDtos.Any())
            {
                foreach (var bookEdition in bookEditionDtos)
                {
                    // Retrieve book authors (if any)
                    bookEdition.BookEditionAuthors = await HandleBookEditionAuthorAddedAsync(auditTrailList, bookEdition.BookEditionId.ToString());
                    // Retrieve book copies (if any)
                    var test = await HandleBookEditionCopyAddedAsync(auditTrailList,
                        bookEdition.BookEditionId.ToString());
                    bookEdition.BookEditionCopies = await HandleBookEditionCopyAddedAsync(auditTrailList, bookEdition.BookEditionId.ToString());
                }
            }

            return bookEditionDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<BookEditionDto>();
    }

    private async Task<List<BookEditionAuthorDto>> HandleBookEditionAuthorAddedAsync(
        List<AuditTrail> auditTrailList,
        string? bookEditionId = null)
    {
        var editionAuthorAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(BookEditionAuthor)) // Retrieve all BookEditionAuthor entity 
            .ToList(); // Convert to list
        if (editionAuthorAudits.Any()) // Exist at least 1 edition
        {
            var bookEditionAuthorDtos = await Task.FromResult(
                auditTrailList
                    .Where(bea => bea.EntityName == nameof(BookEditionAuthor) &&
                                  bea.NewValues.Any() &&
                                  bea.NewValues.ContainsKey("BookEditionId") &&
                                  bea.NewValues["BookEditionId"]?.ToString() == bookEditionId)
                    .Select(bea => new BookEditionAuthorDto()
                    {
                        BookEditionAuthorId = int.Parse(bea.EntityId ?? "0"),
                        BookEditionId = int.Parse(bea.NewValues["BookEditionId"]?.ToString() ?? "0"),
                        AuthorId = int.Parse(bea.NewValues["AuthorId"]?.ToString()!)
                    }).ToList());
            
            // Try to get author detail from DB
            foreach (var bea in bookEditionAuthorDtos)
            {
                bea.Author = (await _authorService.GetByIdAsync(bea.AuthorId)).Data as AuthorDto ?? null!;
            }

            return bookEditionAuthorDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<BookEditionAuthorDto>();
    }

    private async Task<List<BookEditionCopyDto>> HandleBookEditionCopyAddedAsync(
        List<AuditTrail> auditTrailList, string? bookEditionId = null)
    {
        var editionCopyAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(BookEditionCopy)) // Retrieve all BookEditionCopy entity 
            .ToList(); // Convert to list
        if (editionCopyAudits.Any()) // Exist at least 1 edition
        {
            var editionCopyDtos = auditTrailList
                .Where(i => i.EntityName == nameof(BookEditionCopy) && 
                            i.NewValues.Any() &&
                            i.NewValues.ContainsKey("BookEditionId") &&
                            i.NewValues["BookEditionId"]?.ToString() == bookEditionId)
                .Select(bec => new BookEditionCopyDto
                {
                    BookEditionId = int.Parse(bec.NewValues["BookEditionId"]?.ToString() ?? "0"),
                    BookEditionCopyId = int.Parse(bec.EntityId ?? "0"),
                    Code = bec.NewValues["Code"]?.ToString(),
                    Status = bec.NewValues["Status"]?.ToString()!,
                    CreatedAt = DateTime.Parse(bec.NewValues["CreatedAt"]?.ToString()!),
                    UpdatedAt = DateTime.Parse(bec.NewValues["UpdatedAt"]?.ToString() ??
                                               DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    CreatedBy = bec.NewValues["CreatedBy"]?.ToString()!,
                    UpdatedBy = bec.NewValues["UpdatedBy"]?.ToString() ?? null!,
                    IsDeleted = bool.Parse(bec.NewValues["IsDeleted"]?.ToString()!)
                }).ToList();

            if (editionCopyDtos.Any())
            {
                // Try to get copy histories (if any)
                foreach (var editionCopy in editionCopyDtos)
                {
                    editionCopy.CopyConditionHistories = await HandleCopyHistoryAddedAsync(auditTrailList, editionCopy.BookEditionCopyId.ToString());
                }
            }

            return editionCopyDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<BookEditionCopyDto>();
    }

    private async Task<List<CopyConditionHistoryDto>> HandleCopyHistoryAddedAsync(
        List<AuditTrail> auditTrailList, string? bookEditionCopyId = null)
    {
        var copyHistoryAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(CopyConditionHistory)) // Retrieve all CopyConditionHistory entity 
            .ToList(); // Convert to list

        if (copyHistoryAudits.Any()) // Exist at least 1 history
        {
            return await Task.FromResult(
                auditTrailList
                    .Where(ch => ch.EntityName == nameof(CopyConditionHistory) &&
                                 ch.NewValues.Any() &&
                                 ch.NewValues.ContainsKey("BookEditionCopyId") &&
                                 (string.IsNullOrEmpty(bookEditionCopyId) || ch.NewValues["BookEditionCopyId"]?.ToString() == bookEditionCopyId))
                    .Select(ch => new CopyConditionHistoryDto
                    {
                        ConditionHistoryId = int.Parse(ch.EntityId ?? "0"),
                        BookEditionCopyId = int.Parse(ch.NewValues["BookEditionCopyId"]?.ToString() ?? "0"),
                        Condition = ch.NewValues["Condition"]?.ToString()!,
                        CreatedAt = DateTime.Parse(ch.NewValues["CreatedAt"]?.ToString()!),
                        UpdatedAt = DateTime.Parse(ch.NewValues["UpdatedAt"]?.ToString() ??
                                                   DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                        CreatedBy = ch.NewValues["CreatedBy"]?.ToString()!,
                        UpdatedBy = ch.NewValues["UpdatedBy"]?.ToString() ?? null!,
                    }).ToList());
        }
        
        // Return empty collection <- Not found any
        return new List<CopyConditionHistoryDto>();
    }
    #endregion
}