using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.AuditTrail;
using FPTU_ELibrary.Application.Dtos.Authors;
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
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class AuditTrailService : ReadOnlyService<AuditTrail, AuditTrailDto, int>,
    IAuditTrailService<AuditTrailDto>
{
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ICategoryService<CategoryDto> _cateService;
    private readonly ILibraryShelfService<LibraryShelfDto> _libraryShelfService;
    private readonly ILibraryResourceService<LibraryResourceDto> _resourceService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGrpService;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionService;

    public AuditTrailService(
        IAuthorService<AuthorDto> authorService,
        ICategoryService<CategoryDto> cateService,
        ILibraryResourceService<LibraryResourceDto> resourceService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGrpService,
        ILibraryShelfService<LibraryShelfDto> libraryShelfService,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _resourceService = resourceService;
        _authorService = authorService;
        _cateService = cateService;
        _conditionService = conditionService;
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

    public async Task<IServiceResult> GetAllRoleAuditTrailAsync(ISpecification<AuditTrail> spec, bool tracked = true)
    {
        try{
            // Try to parse specification to RoleAuditTrailSpecification
            var roleAuditTrailSpec = spec as RoleAuditTrailSpecification;
            // Check if specification is null
            if (roleAuditTrailSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
            	    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }		
            
            // Count total audits
            var totalAuditWithSpec = await _unitOfWork.Repository<AuditTrail, int>().CountAsync(roleAuditTrailSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalAuditWithSpec / roleAuditTrailSpec.PageSize);
            
            // Set pagination to specification after count total audits 
            if (roleAuditTrailSpec.PageIndex > totalPage 
                || roleAuditTrailSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                roleAuditTrailSpec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            roleAuditTrailSpec.ApplyPaging(
                skip: roleAuditTrailSpec.PageSize * (roleAuditTrailSpec.PageIndex - 1), 
                take: roleAuditTrailSpec.PageSize);
            
            // Add order
            roleAuditTrailSpec.AddOrderBy(a => a.DateUtc);
            
            // Retrieve all audit trail with spec
            var auditEntities = await _unitOfWork.Repository<AuditTrail, int>()
                .GetAllWithSpecAsync(roleAuditTrailSpec);
            
            if (auditEntities.Any()) // Exist data
            {
            	// Convert to dto collection 
                var auditDtos = _mapper.Map<List<AuditTrailDto>>(auditEntities);
                
            	// Pagination result 
            	var paginationResultDto = new PaginatedResultDto<AuditTrailDto>(auditDtos,
                    roleAuditTrailSpec.PageIndex, roleAuditTrailSpec.PageSize, totalPage, totalAuditWithSpec);
            	
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
            throw new Exception("Error invoke when process get all role audit trail");
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
            // var reqDateTime = DateTime.Parse(dateUtc, null, DateTimeStyles.RoundtripKind);
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
                        OldValues = oldData,
                        NewValues = newData
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
                    at.DateUtc.Year == dateUtc.Year &&
                    at.DateUtc.Month == dateUtc.Month &&
                    at.DateUtc.Day == dateUtc.Day &&
                    at.DateUtc.Hour == dateUtc.Hour &&
                    at.DateUtc.Minute == dateUtc.Minute &&
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
        { nameof(LibraryItemInstance), new List<string> { nameof(LibraryItemConditionHistory) } },
        { nameof(LibraryItemResource), new List<string> { nameof(LibraryResource) } }
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
                    CreatedAt = DateTime.Parse(bec.NewValues["CreatedAt"]?.ToString() 
                                               ?? DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    UpdatedAt = DateTime.Parse(bec.NewValues["UpdatedAt"]?.ToString() ??
                                               DateTime.MinValue.ToString(CultureInfo.InvariantCulture)),
                    CreatedBy = bec.NewValues["CreatedBy"]?.ToString() ?? null!,
                    UpdatedBy = bec.NewValues["UpdatedBy"]?.ToString() ?? null!,
                    IsDeleted = bool.TryParse(bec.NewValues["IsDeleted"]?.ToString(), out var validBool) && validBool,
                    IsCirculated = bool.TryParse(bec.NewValues["IsCirculated"]?.ToString(), out var validBool2) && validBool2,
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
            var libItemConditionHisDtos = auditTrailList
                .Where(ch => ch.EntityName == nameof(LibraryItemConditionHistory) &&
                             ch.NewValues.Any() &&
                             ch.NewValues.ContainsKey("LibraryItemInstanceId") &&
                             (string.IsNullOrEmpty(libraryItemInstanceId) ||
                              ch.NewValues["LibraryItemInstanceId"]?.ToString() == libraryItemInstanceId))
                .Select(ch => new LibraryItemConditionHistoryDto
                {
                    ConditionHistoryId = int.Parse(ch.EntityId ?? "0"),
                    LibraryItemInstanceId = int.Parse(ch.NewValues["LibraryItemInstanceId"]?.ToString() ?? "0"),
                    ConditionId = int.Parse(ch.NewValues["ConditionId"]?.ToString() ?? "0"),
                    CreatedAt = DateTime.TryParse(ch.NewValues["CreatedAt"]?.ToString(), out var validCreatedAt)
                        ? validCreatedAt
                        : DateTime.MinValue,
                    UpdatedAt = DateTime.TryParse(ch.NewValues["UpdatedAt"]?.ToString(), out var validUpdatedAt)
                        ? validUpdatedAt
                        : DateTime.MinValue,
                    CreatedBy = ch.NewValues["CreatedBy"]?.ToString() ?? null!,
                    UpdatedBy = ch.NewValues["UpdatedBy"]?.ToString() ?? null!,
                }).ToList();
            
            // Try to retrieve condition 
            foreach (var libItemHis in libItemConditionHisDtos)
            {
                if ((await _conditionService.GetByIdAsync(
                        libItemHis.ConditionId)).Data is LibraryItemConditionDto conditionDto)
                {
                    libItemHis.Condition = conditionDto;
                }
            }
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
            LibraryItemId = int.TryParse(i.EntityId, out var libraryItemId) ? libraryItemId : 0,
            Title = i.NewValues["Title"]?.ToString() ?? string.Empty,
            SubTitle = i.NewValues["SubTitle"]?.ToString(),
            Responsibility = i.NewValues["Responsibility"]?.ToString(),
            Edition = i.NewValues["Edition"]?.ToString(),
            EditionNumber = int.TryParse(i.NewValues["EditionNumber"]?.ToString(), out var editionNumber) ? editionNumber : 0,
            Language = i.NewValues["Language"]?.ToString() ?? string.Empty,
            OriginLanguage = i.NewValues["OriginLanguage"]?.ToString(),
            Summary = i.NewValues["Summary"]?.ToString(),
            CoverImage = i.NewValues["CoverImage"]?.ToString(),
            PublicationYear = int.TryParse(i.NewValues["PublicationYear"]?.ToString(), out var publicationYear) ? publicationYear : 0,
            Publisher = i.NewValues["Publisher"]?.ToString(),
            PublicationPlace = i.NewValues["PublicationPlace"]?.ToString(),
            ClassificationNumber = i.NewValues["ClassificationNumber"]?.ToString() ?? string.Empty,
            CutterNumber = i.NewValues["CutterNumber"]?.ToString() ?? string.Empty,
            Isbn = i.NewValues["Isbn"]?.ToString() ?? string.Empty,
            Ean = i.NewValues["Ean"]?.ToString(),
            EstimatedPrice = decimal.TryParse(i.NewValues["EstimatedPrice"]?.ToString(), out var estimatedPrice) ? estimatedPrice : 0,
            PageCount = int.TryParse(i.NewValues["PageCount"]?.ToString(), out var pageCount) ? pageCount : 0,
            PhysicalDetails = i.NewValues["PhysicalDetails"]?.ToString(),
            Dimensions = i.NewValues["Dimensions"]?.ToString() ?? string.Empty,
            AccompanyingMaterial = i.NewValues["AccompanyingMaterial"]?.ToString(),
            Genres = i.NewValues["Genres"]?.ToString(),
            GeneralNote = i.NewValues["GeneralNote"]?.ToString(),
            BibliographicalNote = i.NewValues["BibliographicalNote"]?.ToString(),
            TopicalTerms = i.NewValues["TopicalTerms"]?.ToString(),
            AdditionalAuthors = i.NewValues["AdditionalAuthors"]?.ToString(),
            ShelfId = int.TryParse(i.NewValues["ShelfId"]?.ToString(), out var shelfId) ? shelfId : 0,
            CategoryId = int.TryParse(i.NewValues["CategoryId"]?.ToString(), out var categoryId) ? categoryId : 0,
            GroupId = int.TryParse(i.NewValues["GroupId"]?.ToString(), out var groupId) ? groupId : 0,
            Status = Enum.TryParse<LibraryItemStatus>(i.NewValues["Status"]?.ToString(), out var status) ? status : LibraryItemStatus.Draft,
            IsDeleted = bool.TryParse(i.NewValues["IsDeleted"]?.ToString(), out var isDeleted) && isDeleted,
            CanBorrow = bool.TryParse(i.NewValues["CanBorrow"]?.ToString(), out var canBorrow) && canBorrow,
            IsTrained = bool.TryParse(i.NewValues["IsTrained"]?.ToString(), out var isTrained) && isTrained,
            TrainedAt = DateTime.TryParse(i.NewValues["TrainedAt"]?.ToString(), out var trainedAt) ? trainedAt : DateTime.MinValue,
            CreatedBy = i.NewValues["CreatedBy"]?.ToString() ?? string.Empty,
            CreatedAt = DateTime.TryParse(i.NewValues["CreatedAt"]?.ToString(), out var createdAt) ? createdAt : DateTime.MinValue,
            UpdatedAt = DateTime.TryParse(i.NewValues["UpdatedAt"]?.ToString(), out var updatedAt) ? updatedAt : DateTime.MinValue,
            UpdatedBy = i.NewValues["UpdatedBy"]?.ToString()
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