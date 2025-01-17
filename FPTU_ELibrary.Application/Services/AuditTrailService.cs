using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AuditTrail;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AuditTrailService : ReadOnlyService<AuditTrail, AuditTrailDto, int>,
    IAuditTrailService<AuditTrailDto>
{
    private readonly ILibraryResourceService<LibraryResourceDto> _resourceService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGrpService;
    private readonly ILibraryShelfService<LibraryShelfDto> _libraryShelfService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly IUserService<UserDto> _userService;
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ICategoryService<CategoryDto> _cateService;

    public AuditTrailService(
        IUserService<UserDto> userService,
        IAuthorService<AuthorDto> authorService,
        ICategoryService<CategoryDto> cateService,
        IEmployeeService<EmployeeDto> employeeService,
        ILibraryResourceService<LibraryResourceDto> resourceService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGrpService,
        ILibraryShelfService<LibraryShelfDto> libraryShelfService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _resourceService = resourceService;
        _authorService = authorService;
        _cateService = cateService;
        _userService = userService;
        _employeeService = employeeService;
        _libraryItemService = libraryItemService;
        _libraryItemGrpService = libraryItemGrpService;
        _libraryShelfService = libraryShelfService;
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
                    case nameof(LibraryItem):
                        newData = (await HandleLibraryItemAddedAsync(auditTrailList));
                        break;
                    case nameof(LibraryResource):
                        newData = await HandleLibraryItemResourceAddedAsync(auditTrailList);
                        break;
                    case nameof(LibraryItemInstance):
                        newData = await HandleLibraryItemInstanceAddedAsync(auditTrailList);
                        break;
                    case nameof(LibraryItemConditionHistory):
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
        { nameof(LibraryItem) , new List<string> { nameof(LibraryItemAuthor), nameof(LibraryItemInstance), nameof(LibraryItemResource)  } },
        { nameof(LibraryItemInstance), new List<string> { nameof(LibraryItemConditionHistory) } }
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
    private async Task<List<LibraryItemResourceDto>> HandleLibraryItemResourceAddedAsync(
        List<AuditTrail> auditTrailList, string? libraryItemId = null)
    {
        var itemResourceAudits = auditTrailList // From audit trail collection
            .Where(br => br.EntityName == nameof(LibraryItemResource)) // Retrieve all LibraryItemResource entity 
            .ToList(); // Convert to list  
        if (itemResourceAudits.Any()) // Exist at least 1 resource 
        {
            var itemResourceDtos = auditTrailList
                .Where(br => br.EntityName == nameof(LibraryItemResource) &&
                             br.NewValues.Any() &&
                             br.NewValues.ContainsKey("LibraryItemId") &&
                             (string.IsNullOrEmpty(libraryItemId) ||
                              br.NewValues["LibraryItemId"]?.ToString() == libraryItemId))
                .Select(br => new LibraryItemResourceDto
                {
                    ResourceId = int.Parse(br.EntityId ?? "0"),
                    LibraryItemResourceId = int.Parse(br.NewValues["LibraryItemResourceId"]?.ToString() ?? "0"),
                    LibraryItemId = int.Parse(br.NewValues["LibraryItemId"]?.ToString() ?? "0"),
                }).ToList();
            
            // Try to get data of resource
            foreach (var src in itemResourceDtos)
            {
                if ((await _resourceService.GetByIdAsync(src.ResourceId)).Data is LibraryResourceDto resourceDto)
                {
                    src.LibraryResource = resourceDto;
                }
            }

            return itemResourceDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<LibraryItemResourceDto>();
    }
    
    private async Task<List<LibraryItemInstanceDto>> HandleLibraryItemInstanceAddedAsync(
        List<AuditTrail> auditTrailList, string? libraryItemId = null)
    {
        var itemInstanceAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(LibraryItemInstance)) // Retrieve all LibraryItemInstance entity 
            .ToList(); // Convert to list
        if (itemInstanceAudits.Any()) // Exist at least 1 instance
        {
            var itemInstanceDtos = auditTrailList
                .Where(i => i.EntityName == nameof(LibraryItemInstance) && 
                            i.NewValues.Any() &&
                            i.NewValues.ContainsKey("LibraryItemId") &&
                            i.NewValues["LibraryItemId"]?.ToString() == libraryItemId)
                .Select(bec => new LibraryItemInstanceDto
                {
                    LibraryItemInstanceId = int.Parse(bec.EntityId ?? "0"),
                    LibraryItemId = int.Parse(bec.NewValues["LibraryItemId"]?.ToString() ?? "0"),
                    Barcode = bec.NewValues["Barcode"]?.ToString()!,
                    Status = bec.NewValues["Status"]?.ToString()!,
                    CreatedAt = DateTime.Parse(bec.NewValues["CreatedAt"]?.ToString()!),
                    UpdatedAt = DateTime.Parse(bec.NewValues["UpdatedAt"]?.ToString() ??
                                               DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    CreatedBy = bec.NewValues["CreatedBy"]?.ToString()!,
                    UpdatedBy = bec.NewValues["UpdatedBy"]?.ToString() ?? null!,
                    IsDeleted = bool.Parse(bec.NewValues["IsDeleted"]?.ToString()!)
                }).ToList();

            if (itemInstanceDtos.Any())
            {
                // Try to get copy histories (if any)
                foreach (var instanceCondition in itemInstanceDtos)
                {
                    instanceCondition.LibraryItemConditionHistories = await HandleLibraryItemInstanceConditionHisAddedAsync(auditTrailList, instanceCondition.LibraryItemInstanceId.ToString());
                }
            }

            return itemInstanceDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<LibraryItemInstanceDto>();
    }

    private async Task<List<LibraryItemConditionHistoryDto>> HandleLibraryItemInstanceConditionHisAddedAsync(
        List<AuditTrail> auditTrailList, string? libraryItemInstanceId = null)
    {
        var copyHistoryAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(LibraryItemConditionHistory)) // Retrieve all LibraryItemConditionHistory entity 
            .ToList(); // Convert to list

        if (copyHistoryAudits.Any()) // Exist at least 1 history
        {
            return await Task.FromResult(
                auditTrailList
                    .Where(ch => ch.EntityName == nameof(LibraryItemConditionHistory) &&
                                 ch.NewValues.Any() &&
                                 ch.NewValues.ContainsKey("LibraryItemInstanceId") &&
                                 (string.IsNullOrEmpty(libraryItemInstanceId) || ch.NewValues["LibraryItemInstanceId"]?.ToString() == libraryItemInstanceId))
                    .Select(ch => new LibraryItemConditionHistoryDto
                    {
                        ConditionHistoryId = int.Parse(ch.EntityId ?? "0"),
                        LibraryItemInstanceId = int.Parse(ch.NewValues["LibraryItemInstanceId"]?.ToString() ?? "0"),
                        Condition = ch.NewValues["Condition"]?.ToString()!,
                        CreatedAt = DateTime.Parse(ch.NewValues["CreatedAt"]?.ToString()!),
                        UpdatedAt = DateTime.Parse(ch.NewValues["UpdatedAt"]?.ToString() ??
                                                   DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                        CreatedBy = ch.NewValues["CreatedBy"]?.ToString()!,
                        UpdatedBy = ch.NewValues["UpdatedBy"]?.ToString() ?? null!,
                    }).ToList());
        }
        
        // Return empty collection <- Not found any
        return new List<LibraryItemConditionHistoryDto>();
    }
    
    private async Task<List<LibraryItemAuthorDto>> HandleLibraryItemAuthorAddedAsync(
        List<AuditTrail> auditTrailList, string? libraryItemId = null)
    {
        var itemAuthorAudits = auditTrailList // From audit trail collection
            .Where(bea => bea.EntityName == nameof(LibraryItemAuthor)) // Retrieve all LibraryItemAuthor entity 
            .ToList(); // Convert to list
        if (itemAuthorAudits.Any()) // Exist at least 1 item
        {
            var itemAuthorDtos = await Task.FromResult(
                auditTrailList
                    .Where(bea => bea.EntityName == nameof(LibraryItemAuthor) &&
                                  bea.NewValues.Any() &&
                                  bea.NewValues.ContainsKey("LibraryItemId") &&
                                  bea.NewValues["LibraryItemId"]?.ToString() == libraryItemId)
                    .Select(bea => new LibraryItemAuthorDto()
                    {
                        LibraryItemAuthorId = int.Parse(bea.EntityId ?? "0"),
                        LibraryItemId = int.Parse(bea.NewValues["LibraryItemId"]?.ToString() ?? "0"),
                        AuthorId = int.Parse(bea.NewValues["AuthorId"]?.ToString()!)
                    }).ToList());
            
            // Try to get author detail from DB
            foreach (var bea in itemAuthorDtos)
            {
                bea.Author = (await _authorService.GetByIdAsync(bea.AuthorId)).Data as AuthorDto ?? null!;
            }

            return itemAuthorDtos;
        }
        
        // Return empty collection <- Not found any
        return new List<LibraryItemAuthorDto>();
    }
    
    private async Task<LibraryItemDto?> HandleLibraryItemAddedAsync(List<AuditTrail> auditTrailList)
    {
        var itemAudit = auditTrailList // From audit trail collection
            .Where(a => a.EntityName == nameof(LibraryItem))
            .Select(i => new LibraryItemDto
            {
                LibraryItemId = int.Parse(i.EntityId ?? "0"),
                Title = i.NewValues["Title"]?.ToString()!,
                SubTitle = i.NewValues["SubTitle"]?.ToString(),
                Responsibility = i.NewValues["Responsibility"]?.ToString(),
                Edition = i.NewValues["Edition"]?.ToString(),
                EditionNumber = int.Parse(i.NewValues["EditionNumber"]?.ToString()!),
                Language = i.NewValues["Language"]?.ToString()!,
                OriginLanguage = i.NewValues["OriginLanguage"]?.ToString(),
                Summary = i.NewValues["Summary"]?.ToString(),
                CoverImage = i.NewValues["CoverImage"]?.ToString(),
                PublicationYear = int.Parse(i.NewValues["PublicationYear"]?.ToString()!),
                Publisher = i.NewValues["Publisher"]?.ToString(),
                PublicationPlace = i.NewValues["PublicationPlace"]?.ToString(),
                ClassificationNumber = i.NewValues["ClassificationNumber"]?.ToString()!,
                CutterNumber = i.NewValues["CutterNumber"]?.ToString()!,
                Isbn = i.NewValues["Isbn"]?.ToString()!,
                Ean = i.NewValues["Ean"]?.ToString(),
                EstimatedPrice = decimal.Parse(i.NewValues["EstimatedPrice"]?.ToString()!),
                PageCount = int.Parse(i.NewValues["PageCount"]?.ToString()!),
                PhysicalDetails = i.NewValues["PhysicalDetails"]?.ToString(),
                Dimensions = i.NewValues["Dimensions"]?.ToString()!,
                AccompanyingMaterial = i.NewValues["AccompanyingMaterial"]?.ToString(),
                Genres = i.NewValues["Genres"]?.ToString(),
                GeneralNote = i.NewValues["GeneralNote"]?.ToString(),
                BibliographicalNote = i.NewValues["BibliographicalNote"]?.ToString(),
                TopicalTerms = i.NewValues["TopicalTerms"]?.ToString(),
                AdditionalAuthors = i.NewValues["AdditionalAuthors"]?.ToString(),
                ShelfId = int.Parse(i.NewValues["ShelfId"]?.ToString() ?? "0"),
                CategoryId = int.Parse(i.NewValues["CategoryId"]?.ToString() ?? "0"),
                GroupId = int.Parse(i.NewValues["GroupId"]?.ToString() ?? "0"),
                Status = (LibraryItemStatus) Enum.Parse(typeof(LibraryItemStatus), i.NewValues["Status"]?.ToString()!),
                IsDeleted = bool.Parse(i.NewValues["IsDeleted"]?.ToString()!),
                CanBorrow = bool.Parse(i.NewValues["CanBorrow"]?.ToString()!),
                IsTrained = bool.Parse(i.NewValues["IsTrained"]?.ToString()!),
                TrainedAt = DateTime.Parse(i.NewValues["TrainedAt"]?.ToString() ??
                                           DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                CreatedBy = i.NewValues["CreatedBy"]?.ToString()!,
                CreatedAt = DateTime.Parse(i.NewValues["CreatedAt"]?.ToString()!),
                UpdatedAt = DateTime.Parse(i.NewValues["UpdatedAt"]?.ToString() ??
                                           DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                UpdatedBy = i.NewValues["UpdatedBy"]?.ToString() ?? null!,
            })
            .FirstOrDefault(); // Retrieve LibraryItem entity 
        
        if (itemAudit != null) // Exist item
        {
            // Try to retrieve reference data
            if ((await _cateService.GetByIdAsync(itemAudit.CategoryId)).Data is CategoryDto categoryDto)
            {
                itemAudit.Category = categoryDto;
            }
            if ((await _libraryShelfService.GetByIdAsync(itemAudit.ShelfId ?? 0)).Data is LibraryShelfDto shelfDto)
            {
                itemAudit.Shelf = shelfDto;
            }
            if ((await _libraryItemGrpService.GetByIdAsync(itemAudit.GroupId ?? 0)).Data is LibraryItemGroupDto groupDto)
            {
                itemAudit.LibraryItemGroup = groupDto;
            }
            
            // Try to retrieve navigations data
            itemAudit.LibraryItemResources = await HandleLibraryItemResourceAddedAsync(auditTrailList, itemAudit.LibraryItemId.ToString());
            itemAudit.LibraryItemInstances = await HandleLibraryItemInstanceAddedAsync(auditTrailList, itemAudit.LibraryItemId.ToString());
            itemAudit.LibraryItemAuthors = await HandleLibraryItemAuthorAddedAsync(auditTrailList, itemAudit.LibraryItemId.ToString());
            
            return itemAudit;
        }
        
        // Return empty collection <- Not found any
        return null;
    }
    #endregion
}