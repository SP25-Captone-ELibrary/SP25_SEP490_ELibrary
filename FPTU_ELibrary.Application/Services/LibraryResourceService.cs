using System.Diagnostics;
using System.IO.Pipes;
using Amazon.S3.Model;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using iTextSharp.text;
using iTextSharp.text.pdf;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NAudio.Lame;
using NAudio.Wave;
using Serilog;
using Xabe.FFmpeg;

namespace FPTU_ELibrary.Application.Services;

public class LibraryResourceService : GenericService<LibraryResource, LibraryResourceDto, int>,
    ILibraryResourceService<LibraryResourceDto>
{
    private readonly IEmployeeService<EmployeeDto> _empService;
    private readonly IUserService<UserDto> _userService;
    private readonly FFMPEGSettings _ffmpegSettings;
    private readonly IVoiceService _voiceService;
    private readonly IS3Service _s3Service;
    private readonly DigitalResourceSettings _digitalSettings;
    private readonly ICloudinaryService _cloudService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;

    public LibraryResourceService(
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ICloudinaryService cloudService,
        IEmployeeService<EmployeeDto> empService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserService<UserDto> userService,
        HttpClient client,
        IOptionsMonitor<DigitalResourceSettings> digitalSettings,
        IOptionsMonitor<FFMPEGSettings> ffmpegSettings,
        IVoiceService voiceService,
        IS3Service s3Service,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _empService = empService;
        _userService = userService;
        _ffmpegSettings = ffmpegSettings.CurrentValue;
        _voiceService = voiceService;
        _s3Service = s3Service;
        _digitalSettings = digitalSettings.CurrentValue;
        _cloudService = cloudService;
        _libraryItemService = libraryItemService;
    }
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<LibraryResource> specification,
        bool tracked = true)
    {
        // Try to parse specification to BookResourceSpecification
        var resourceSpec = specification as LibraryResourceSpecification;
        // Check if specification is null
        if (resourceSpec == null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }

        // Count total resource
        var totalResourceWithSpec = await _unitOfWork.Repository<LibraryResource, int>().CountAsync(resourceSpec);
        // Count total page
        var totalPage = (int)Math.Ceiling((double)totalResourceWithSpec / resourceSpec.PageSize);

        // Set pagination to specification after count total resource 
        if (resourceSpec.PageIndex > totalPage
            || resourceSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
        {
            resourceSpec.PageIndex = 1; // Set default to first page
        }

        // Apply pagination
        resourceSpec.ApplyPaging(
            skip: resourceSpec.PageSize * (resourceSpec.PageIndex - 1),
            take: resourceSpec.PageSize);

        // Get all with spec
        var entities = await _unitOfWork.Repository<LibraryResource, int>()
            .GetAllWithSpecAsync(resourceSpec, tracked);

        if (entities.Any()) // Exist data
        {
            // Convert to dto collection 
            var resourceDtos = (_mapper.Map<IEnumerable<LibraryResourceDto>>(entities)).ToListSecureLibraryResourceDto();

            // Pagination result 
            var paginationResultDto = new PaginatedResultDto<SecureLibraryResourceDto>(resourceDtos,
                resourceSpec.PageIndex, resourceSpec.PageSize, totalPage, totalResourceWithSpec);

            // Response with pagination 
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
        }

        // Not found any data
        return new ServiceResult(ResultCodeConst.SYS_Warning0004,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
            // Mapping entities to dto 
            (_mapper.Map<IEnumerable<LibraryResourceDto>>(entities)).ToListSecureLibraryResourceDto());
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec query
            var baseSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                // Include digital borrows
                .Include(lr => lr.DigitalBorrows));
            // Retrieve entity with spec
            var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
                .GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<LibraryResourceDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get data");
        }
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build a base specification to filter by ResourceId
            var baseSpec = new BaseSpecification<LibraryResource>(s => s.ResourceId == id);

            // Retrieve resource with specification
            var resourceEntity = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(baseSpec);
            if (resourceEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            // Check whether resource in the trash bin
            if (!resourceEntity.IsDeleted)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            if(resourceEntity.ResourceType=="Video"&& resourceEntity.S3OriginalName.IsNullOrEmpty())
            {
                // Process delete in s3
                await _s3Service.DeleteFileAsync(AudioResourceType.Original, resourceEntity.S3OriginalName!);
            }
            // Process add delete entity
            await _unitOfWork.Repository<LibraryResource, int>().DeleteAsync(id);
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

    public async Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<LibraryResource> spec)
    {
        return await base.GetAllWithSpecAsync(spec);
    }

    public async Task<IServiceResult> AddResourceToLibraryItemAsync(int libraryItemId, LibraryResourceDto dto)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get file type
            if (Enum.TryParse(typeof(FileType), dto.FileFormat, out var fileType))
            {
                // Determine file type
                if ((FileType)fileType == FileType.Image)
                {
                    // Check exist resource
                    var checkExistResult = await _cloudService.IsExistAsync(dto.ProviderPublicId, (FileType)fileType!);
                    if (checkExistResult.Data is false) // Return when not found resource on cloud
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Warning0003,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0003));
                    }
                }
                else if ((FileType)fileType == FileType.Video)
                {
                    if ((await _s3Service.GetFileUrlAsync(AudioResourceType.Original, dto.S3OriginalName!))
                        .Data is string url)
                    {
                        dto.ResourceUrl = url;
                    }
                    else
                    {
                        // Msg: Fail to upload video
                        return new ServiceResult(ResultCodeConst.Cloud_Fail0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0002));
                    }
                }
            }
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Check exist item
            var isItemExist = (await _libraryItemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isItemExist)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "item to process add resource"
                        : "tài liệu để thêm mới tài nguyên"));
            }
            else
            {
                // Check not same publicId and resourceUrl
                var isDuplicateContent = await _unitOfWork.Repository<LibraryResource, int>().AnyAsync(x =>
                    x.ProviderPublicId == dto.ProviderPublicId || // With specific public id
                    x.ResourceUrl == dto.ResourceUrl); // with specific resource url
                if (isDuplicateContent) // Not allow to have same resource content
                {
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0003));
                }
            }

            // Generate new library item resource
            var libResource = new LibraryItemResource()
            {
                LibraryItemId = libraryItemId,
                LibraryResource = _mapper.Map<LibraryResource>(dto)
            };

            // Process add new entity
            await _unitOfWork.Repository<LibraryItemResource, int>().AddAsync(libResource);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }
            else
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add item resource");
        }
    }

    public async Task<IServiceResult> AddResourceToLibraryItemAsync(int libraryItemId, LibraryResourceDto dto,
        Dictionary<int, string> chunkDetails)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            // Add represent url and providerId
            dto.ResourceUrl = chunkDetails[chunkDetails.Keys.Min()];
            dto.ProviderPublicId = StringUtils.GetPublicIdFromCloudinaryUrl(dto.ResourceUrl);
            dto.ResourceType = ResourceType.BookAudio.ToString();
            List<LibraryResourceUrlDto> urls = new List<LibraryResourceUrlDto>();
            foreach (var chunkDetail in chunkDetails)
            {
                urls.Add(new()
                {
                    Url = chunkDetail.Value,
                    PartNumber = chunkDetail.Key
                });
            }

            dto.LibraryResourceUrls = urls;


            // // Validate inputs using the generic validator
            // var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // // Check for valid validations
            // if (validationResult != null && !validationResult.IsValid)
            // {
            //     // Convert ValidationResult to ValidationProblemsDetails.Errors
            //     var errors = validationResult.ToProblemDetails().Errors;
            //     throw new UnprocessableEntityException("Invalid Validations", errors);
            // }

            // Check exist item
            var isItemExist = (await _libraryItemService.AnyAsync(x => x.LibraryItemId == libraryItemId)).Data is true;
            if (!isItemExist)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "item to process add resource"
                        : "tài liệu để thêm mới tài nguyên"));
            }
            else
            {
                // Check not same publicId and resourceUrl
                var isDuplicateContent = await _unitOfWork.Repository<LibraryResource, int>().AnyAsync(x =>
                    x.ProviderPublicId == dto.ProviderPublicId || // With specific public id
                    x.ResourceUrl == dto.ResourceUrl); // with specific resource url
                if (isDuplicateContent) // Not allow to have same resource content
                {
                    return new ServiceResult(ResultCodeConst.LibraryItem_Warning0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0003));
                }

                // Check resource format
                if (!dto.ResourceUrl.Contains(dto.ProviderPublicId)) // Invalid resource format
                {
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
                }

                // Get file type
                Enum.TryParse(typeof(FileType), dto.FileFormat, out var fileType);
                // Check exist resource
                // var checkExistResult = await _cloudService.IsExistAsync(dto.ProviderPublicId, (FileType)fileType!);
                // if (checkExistResult.Data is false) return checkExistResult; // Return when not found resource on cloud
            }

            // Generate new library item resource
            var libResource = new LibraryItemResource()
            {
                LibraryItemId = libraryItemId,
                LibraryResource = _mapper.Map<LibraryResource>(dto)
            };

            // Process add new entity
            await _unitOfWork.Repository<LibraryItemResource, int>().AddAsync(libResource);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }
            else
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add item resource");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, LibraryResourceDto dto)
    {
        try
        {
            // Determine lang context
            var lang =
                (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
                    .CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;

                // Check if errors contain specific fields (skip for update)
                if (errors.TryGetValue(StringUtils.ToCamelCase(nameof(LibraryResource.ResourceType)), out _))
                {
                    errors.Remove(StringUtils.ToCamelCase(nameof(LibraryResource.ResourceType)));
                }

                if (errors.Keys.Any())
                {
                    throw new UnprocessableEntityException("Invalid Validations", errors);
                }
            }

            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
                .GetWithSpecAsync(new BaseSpecification<LibraryResource>(x => x.ResourceId == id));
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item resource" : "tài nguyên"));
            }

            // Check incorrect update
            if (existingEntity.ProviderPublicId != dto.ProviderPublicId || // Not allow to update provider id
                existingEntity.FileFormat != dto.FileFormat || // Update with other file format
                !dto.ResourceUrl.Contains(dto.ProviderPublicId)) // Invalid resource url 
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }

            // Get file type
            Enum.TryParse(typeof(FileType), dto.FileFormat, out var fileType);
            // Check exist resource
            var checkExistResult = await _cloudService.IsExistAsync(dto.ProviderPublicId, (FileType)fileType!);
            if (checkExistResult.Data is false) return checkExistResult; // Not found resource on cloud

            // Process update resource properties
            existingEntity.ResourceTitle = dto.ResourceTitle;
            existingEntity.FileFormat = dto.FileFormat;
            existingEntity.ResourceUrl = dto.ResourceUrl;
            existingEntity.ResourceSize = dto.ResourceSize;
            existingEntity.DefaultBorrowDurationDays = dto.DefaultBorrowDurationDays;
            existingEntity.BorrowPrice = dto.BorrowPrice;

            // Progress update when all require passed
            await _unitOfWork.Repository<LibraryResource, int>().UpdateAsync(existingEntity);

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
            throw new Exception("Error invoke when process update item resource");
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

            // Build spec query
            var baseSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                // Include digital borrows
                .Include(lr => lr.DigitalBorrows));
            // Retrieve entity with spec
            var existingEntity = await _unitOfWork.Repository<LibraryResource, int>()
                .GetWithSpecAsync(baseSpec);
            // Check if resource already mark as deleted
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            // Check whether resource is borrowed
            var hasConstraint =
                existingEntity.DigitalBorrows.Any(db =>
                    // Any digital borrow is currently activating
                    db.Status == BorrowDigitalStatus.Active || // OR
                    // Has not expired yet
                    db.ExpiryDate.Date > DateTime.Now.Date
                );
            if (hasConstraint) // Has constraint with other relations
            {
                return new ServiceResult(ResultCodeConst.LibraryItem_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008));
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
            throw new Exception("Error invoke when process soft delete resources");
        }
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] ids)
    {
        try
        {
            // Get all matching resource
            // Build spec
            var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
            // Apply include
            baseSpec.ApplyInclude(q => q
                // Include digital borrows
                .Include(lr => lr.DigitalBorrows));
            // Retrieve all data with spec
            var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var resourceList = resourceEntities.ToList();
            if (!resourceList.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            if (resourceList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Add custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Progress update deleted status to true
            for (int i = 0; i < resourceList.Count; ++i)
            {
                var src = resourceList[i];

                // Check whether resource is borrowed or not 
                var hasConstraint =
                    src.DigitalBorrows.Any(db =>
                        // Any digital borrow is currently activating
                        db.Status == BorrowDigitalStatus.Active || // OR
                        // Has not expired yet
                        db.ExpiryDate.Date > DateTime.Now.Date
                    );
                if (hasConstraint) // Has constraint with other relations
                {
                    // Add error
                    customErrs.Add($"ids[{i}]",
                        [await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0008)]);
                }
            }

            if (customErrs.Any()) // Invoke errors
            {
                throw new UnprocessableEntityException("Invalid data", customErrs);
            }


            // Progress update deleted status to true
            resourceList.ForEach(x => x.IsDeleted = true);

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
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when remove range resource");
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

            // Check exist resource
            var existingEntity = await _unitOfWork.Repository<LibraryResource, int>().GetByIdAsync(id);
            // Check if resource already mark as deleted
            if (existingEntity == null || !existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            // Update delete status
            existingEntity.IsDeleted = false;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
            }

            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0009,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process undo delete resource");
        }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] ids)
    {
        try
        {
            // Get all matching resource 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
            var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var resourceList = resourceEntities.ToList();
            if (resourceList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
            }

            // Progress undo deleted status to false
            resourceList.ForEach(x => x.IsDeleted = false);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                // Get error msg
                return new ServiceResult(ResultCodeConst.SYS_Fail0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0009));
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
            // Get all matching resource 
            // Build spec
            var baseSpec = new BaseSpecification<LibraryResource>(e => ids.Contains(e.ResourceId));
            var resourceEntities = await _unitOfWork.Repository<LibraryResource, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var resourceList = resourceEntities.ToList();
            if (resourceList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process delete range
            await _unitOfWork.Repository<LibraryResource, int>().DeleteRangeAsync(ids);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, resourceList.Count.ToString()), true);
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
            throw new Exception("Error invoke when process delete range resource");
        }
    }

    public async Task<IServiceResult<(Stream, string)>> GetFullPdfFileWithWatermark(string email, int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            resourceSpec.EnableSplitQuery();
            resourceSpec.ApplyInclude(q => q.Include(r => r.LibraryResourceUrls));
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult<(Stream, string)>(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            // Check if this email is available to have this resource
            var userSpec = new BaseSpecification<User>(u => u.Email.Equals(email)
                                                            && u.DigitalBorrows.Any(db =>
                                                                db.LibraryResource.ResourceId == resourceId));
            userSpec.ApplyInclude(q => q
                .Include(u => u.DigitalBorrows)
                .ThenInclude(db => db.LibraryResource));
            var user = await _userService.GetWithSpecAsync(userSpec);
            if (user.Data is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0018);
                return new ServiceResult<(Stream, string)>(ResultCodeConst.Borrow_Warning0018,
                    StringUtils.Format(errMsg, isEng
                        ? "user has not borrowed this resource"
                        : "người dùng chưa mượn tài nguyên này"));
            }

            var userValue = (user.Data as UserDto)!;
            var userBorrows = userValue.DigitalBorrows.FirstOrDefault(db => db.LibraryResource.ResourceId == resourceId
                                                                            && db.Status == BorrowDigitalStatus.Active
                                                                            && db.ExpiryDate.Date > DateTime.Now.Date);
            if (userBorrows is null)
            {
                return new ServiceResult<(Stream, string)>(ResultCodeConst.Borrow_Warning0019,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0019));
            }

            // resource.FileFormat.ToLower().Equals("pdf    ")
            if (resource.FileFormat.ToLower().Equals("image"))
            {
                var resourceUrl = resource.ResourceUrl;

                // if (pageNumber == 0 || pageNumber is null) pageNumber = 1;
                var pdfStream = await DownloadAndAddWatermark(resourceUrl, email, false);
                return new ServiceResult<(Stream, string)>(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    (pdfStream, resource.FileFormat.ToLower()));
            }

            return new ServiceResult<(Stream, string)>(ResultCodeConst.SYS_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource");
        }
    }

    #region mp3 v1

    public async Task<IServiceResult> GetFullAudioFileWithWatermark(string email, int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            resourceSpec.EnableSplitQuery();
            resourceSpec.ApplyInclude(q => q.Include(r => r.LibraryResourceUrls));
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            // Check if this email is available to have this resource
            var userSpec = new BaseSpecification<User>(u => u.Email.Equals(email)
                                                            && u.DigitalBorrows.Any(db =>
                                                                db.LibraryResource.ResourceId == resourceId));
            userSpec.ApplyInclude(q => q
                .Include(u => u.DigitalBorrows)
                .ThenInclude(db => db.LibraryResource));
            var user = await _userService.GetWithSpecAsync(userSpec);
            if (user.Data is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0018);
                return new ServiceResult(ResultCodeConst.Borrow_Warning0018,
                    StringUtils.Format(errMsg, isEng
                        ? "user has not borrowed this resource"
                        : "người dùng chưa mượn tài nguyên này"));
            }

            var userValue = (user.Data as UserDto)!;
            var userBorrows = userValue.DigitalBorrows.FirstOrDefault(db => db.LibraryResource.ResourceId == resourceId
                                                                            && db.Status == BorrowDigitalStatus.Active
                                                                            && db.ExpiryDate.Date > DateTime.Now.Date);
            if (userBorrows is null || userBorrows.S3WatermarkedName.IsNullOrEmpty())
            {
                return new ServiceResult(ResultCodeConst.Borrow_Warning0019,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0019));
            }

            var returnUrl =
                (await _s3Service.GetFileUrlAsync(AudioResourceType.Watermarked, userBorrows.S3WatermarkedName!)).Data as string;

            // Return the part of the stream
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                returnUrl);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource");
        }
    }

    #endregion

    public async Task<IServiceResult> GetNumberOfUploadAudioFile(string email, int resourceId)
    {
        // Determine current system language
        var sysLang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = sysLang == SystemLanguage.English;

        // Get Speech Size
        var textToVoice = await _voiceService.TextToVoice(LanguageContext.CurrentLanguage, email);
        var audioStream = (MemoryStream)textToVoice.Data!;
        var speechSize = audioStream.Length;

        //Get Audio Size
        var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
        resourceSpec.EnableSplitQuery();
        resourceSpec.ApplyInclude(q => q.Include(r => r.LibraryResourceUrls));
        var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
        if (resource is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
        }

        var times = (resource.ResourceSize * 1024 * 1024! + speechSize) / (40 * 1024 * 1024);

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            Math.Ceiling(times ?? 0));
    }

    public async Task<IServiceResult<Stream>> GetAudioPreviewFromAws(int resourceId)
    {
        string tempAudioPath = Path.Combine(Path.GetTempPath(), $"original_{Path.GetRandomFileName()}.mp3");
        string trimmedPath = Path.Combine(Path.GetTempPath(), $"preview_{Path.GetRandomFileName()}.mp3");

        try
        {
            var sysLang =
                (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
                    .CurrentLanguage);
            var isEng = sysLang == SystemLanguage.English;

            var baseSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(baseSpec);

            if (resource is null || string.IsNullOrEmpty(resource.S3OriginalName))
            {
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002));
            }

            var originAudioResponse =
                (await _s3Service.GetFileAsync(AudioResourceType.Original, resource.S3OriginalName)).Data as
                GetObjectResponse;

            if (originAudioResponse == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource in s3" : "tài nguyên trong s3"));
            }

            // Save original S3 audio to disk
            await using (var originalAudioStream = originAudioResponse.ResponseStream)
            await using (var fileStream = new FileStream(tempAudioPath, FileMode.Create, FileAccess.Write))
            {
                await originalAudioStream.CopyToAsync(fileStream);
            }

            // Trim first 15 seconds
            TimeSpan start = TimeSpan.Zero;
            TimeSpan end = TimeSpan.FromSeconds(5*60);
            TrimMp3(tempAudioPath, trimmedPath, start, end);

            var previewBytes = await File.ReadAllBytesAsync(trimmedPath);
            var previewStream = new MemoryStream(previewBytes);

            return new ServiceResult<Stream>(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), previewStream);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error while processing audio preview");
        }
        finally
        {
            try
            {
                if (File.Exists(tempAudioPath)) File.Delete(tempAudioPath);
                if (File.Exists(trimmedPath)) File.Delete(trimmedPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.Warning("Failed to delete temp file: {Message}", cleanupEx.Message);
            }
        }
    }

    public async Task<IServiceResult> WatermarkAudioAsyncFromAWS(string? s3OriginalAudioName, string email)
    {
        // Determine current system language
        var sysLang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = sysLang == SystemLanguage.English;

        string tempDirectory = Path.GetTempPath();
        string tempAudioPath = Path.Combine(tempDirectory, $"original_{Path.GetRandomFileName()}.mp3");
        string tempWavPath = Path.Combine(tempDirectory, $"processed_{Path.GetRandomFileName()}.wav");
        string tempMp3Path = Path.Combine(tempDirectory, $"final_{Path.GetRandomFileName()}.mp3");
        var user = (await _userService.GetByEmailAsync(email)).Data as UserDto;
        if (user is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
        }

        try
        {
            var originAudioResponse =
                (await _s3Service.GetFileAsync(AudioResourceType.Original, s3OriginalAudioName)).Data as
                GetObjectResponse;
            if (originAudioResponse == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource in s3" : "tài nguyên trong s3"));
            }

            await using (var originalAudioStream = originAudioResponse.ResponseStream)
            await using (var fileStream = new FileStream(tempAudioPath, FileMode.Create, FileAccess.Write))
            {
                await originalAudioStream.CopyToAsync(fileStream);
            }

            _logger.Information("Generating watermark from text-to-speech");
            var watermarkResult = await _voiceService.TextToVoiceFile(email);
            if (watermarkResult.Data is not MemoryStream watermarkStream)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            await using (var originalAudioReader = new Mp3FileReader(tempAudioPath))
            await using (var writer = new WaveFileWriter(tempWavPath, originalAudioReader.WaveFormat))
            await using (var watermarkReader = new WaveFileReader(watermarkStream))
            await using (var conversionStream = new WaveFormatConversionStream(writer.WaveFormat, watermarkReader))
            {
                int bytesPerSecond = originalAudioReader.WaveFormat.AverageBytesPerSecond;
                int insertInterval = 15 * 60 * bytesPerSecond;
                const int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];
                long totalBytesRead = 0;
                int count = 0;

                // Chèn watermark ngay đầu file
                InsertWatermark(writer, conversionStream);
                _logger.Information("Inserted watermark at start (0 minutes)");

                int bytesRead;
                while ((bytesRead = originalAudioReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    if (totalBytesRead >= insertInterval)
                    {
                        totalBytesRead = 0;
                        InsertWatermark(writer, conversionStream);
                        count++;
                        _logger.Information("Inserted watermark at {Minutes} minutes", 15 * count);
                    }
                }

                writer.Flush();
            }

            _logger.Information("Converting WAV to mp3");
            await using (var wavReader = new WaveFileReader(tempWavPath))
            await using (var mp3Stream = new FileStream(tempMp3Path, FileMode.Create, FileAccess.ReadWrite))
            await using (var mp3Writer = new LameMP3FileWriter(mp3Stream, wavReader.WaveFormat, LAMEPreset.VBR_90))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = wavReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    mp3Writer.Write(buffer, 0, bytesRead);
                }

                mp3Writer.Flush();
            }

            _logger.Information("Successfully converted to MP3");

            // Tải file MP3 đã xử lý lên S3
            _logger.Information("Uploading watermarked MP3 to S3");
            await using (var mp3FileStream = new FileStream(tempMp3Path, FileMode.Open, FileAccess.Read))
            {
                var watermarkedFileName = $"{s3OriginalAudioName}_{user.UserId}";
                await _s3Service.UploadFileAsync(AudioResourceType.Watermarked, mp3FileStream,
                    watermarkedFileName);
                return new ServiceResult(ResultCodeConst.Cloud_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002), watermarkedFileName);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource from s3");
        }
        finally
        {
            try
            {
                if (File.Exists(tempAudioPath))
                    File.Delete(tempAudioPath);

                if (File.Exists(tempWavPath))
                    File.Delete(tempWavPath);

                if (File.Exists(tempMp3Path))
                    File.Delete(tempMp3Path);
            }
            catch (Exception cleanupEx)
            {
                _logger.Warning("Failed to clean up temp files: {Message}", cleanupEx.Message);
            }
        }
    }

    private async Task<IServiceResult<Stream>> AddAudioWatermark(int resourceId, string resourceLang, string email)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var ffmpegPath = _ffmpegSettings.Path;
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new Exception("FFmpeg path is not configured.");
            }

            var ffmpegExePath = Path.Combine(ffmpegPath, "ffmpeg.exe");

            var audioFileResult = await GetFullLargeFile(resourceId);

            var ttsResult = await _voiceService.TextToVoiceFile(email);

            var tempDir = Path.Combine(Path.GetTempPath(), "AudioProcessing");
            Directory.CreateDirectory(tempDir);

            var audioBookPath = Path.Combine(tempDir, "audio-book.mp3");
            var watermarkPath = Path.Combine(tempDir, "watermark.mp3");
            var concatListPath = Path.Combine(tempDir, "concat_list.txt");
            var outputPath = Path.Combine(tempDir, "output.mp3");

            await File.WriteAllBytesAsync(audioBookPath, ((MemoryStream)audioFileResult.Data!).ToArray());
            await File.WriteAllBytesAsync(watermarkPath, ((MemoryStream)ttsResult.Data!).ToArray());

            double audioBookDuration = GetMp3Duration(audioBookPath);
            double watermarkDuration = GetMp3Duration(watermarkPath);
            int chunkInterval = 20 * 60; // 10 phút (600 giây)
            int numberOfSegments = (int)Math.Ceiling(audioBookDuration / chunkInterval);

            List<string> segmentFiles = new();
            for (int i = 0; i < numberOfSegments; i++)
            {
                string segmentFile = Path.Combine(tempDir, $"segment_{i}.mp3");
                double startTime = i * chunkInterval;
                double duration = Math.Min(chunkInterval, audioBookDuration - startTime);

                var splitProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegExePath,
                        Arguments = $"-i \"{audioBookPath}\" -ss {startTime} -t {duration} -c copy \"{segmentFile}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                splitProcess.Start();
                await splitProcess.WaitForExitAsync();

                if (splitProcess.ExitCode != 0)
                {
                    return new ServiceResult<Stream>(ResultCodeConst.SYS_Fail0002, "Failed to split audio file");
                }

                segmentFiles.Add(segmentFile);
            }

            using (var writer = new StreamWriter(concatListPath))
            {
                await writer.WriteLineAsync($"file '{watermarkPath}'");
                for (int i = 0; i < segmentFiles.Count; i++)
                {
                    await writer.WriteLineAsync($"file '{segmentFiles[i]}'");
                    if (i < segmentFiles.Count - 1)
                    {
                        await writer.WriteLineAsync($"file '{watermarkPath}'");
                    }
                }
            }

            var concatProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegExePath,
                    Arguments = $"-f concat -safe 0 -i \"{concatListPath}\" -c:a mp3 -b:a 192k \"{outputPath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            concatProcess.Start();
            string errorOutput = await concatProcess.StandardError.ReadToEndAsync();
            await concatProcess.WaitForExitAsync();

            if (concatProcess.ExitCode != 0)
            {
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Fail0002, $"FFmpeg failed: {errorOutput}");
            }

            var finalStream = new MemoryStream(await File.ReadAllBytesAsync(outputPath));

            CleanupTempFiles(tempDir);

            stopwatch.Stop();
            Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds}ms");

            return new ServiceResult<Stream>(ResultCodeConst.SYS_Success0002, "Successfully added watermark",
                finalStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return new ServiceResult<Stream>(ResultCodeConst.SYS_Fail0002, "Error processing audio");
        }
    }

    private void CleanupTempFiles(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up temp files: {ex.Message}");
        }
    }

    private double GetMp3Duration(string filePath)
    {
        using var reader = new MediaFoundationReader(filePath);
        return reader.TotalTime.TotalSeconds;
    }

    private async Task<IServiceResult<Stream>> GetFullLargeFile(int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            resourceSpec.EnableSplitQuery();
            resourceSpec.ApplyInclude(q => q.Include(r => r.LibraryResourceUrls));
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            var listUrls = resource.LibraryResourceUrls.Select(u => u.Url).ToList();
            var fullFileStream = await DownloadAndMergeFileChunks(listUrls);
            return new ServiceResult<Stream>(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), fullFileStream);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource preview");
        }
    }

    private async Task<MemoryStream> DownloadAndMergeFileChunks(List<string> urls)
    {
        var combinedStream = new MemoryStream();

        using (var httpClient = new HttpClient())
        {
            foreach (var url in urls)
            {
                // Tải phần của file từ URL
                var fileBytes = await httpClient.GetByteArrayAsync(url);
                combinedStream.Write(fileBytes, 0, fileBytes.Length);
            }
        }

        // Đặt con trỏ về đầu stream để có thể đọc khi trả về
        combinedStream.Seek(0, SeekOrigin.Begin);
        return combinedStream;
    }

    public async Task<IServiceResult> GetFullOriginalAudio(int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }
            var returnUrl = (await _s3Service.GetFileUrlAsync(AudioResourceType.Original, resource.S3OriginalName!)).Data as string;
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), returnUrl);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource");
        }
    }

    public async Task<IServiceResult<MemoryStream>> GetAudioPreview(int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult<MemoryStream>(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            using HttpClient httpClient = new HttpClient();
            byte[] fileBytes = await httpClient.GetByteArrayAsync(resource.ResourceUrl);
            MemoryStream stream = new MemoryStream(fileBytes);

            return new ServiceResult<MemoryStream>(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), stream);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource");
        }
    }

    public async Task<IServiceResult<Stream>> GetPdfPreview(int resourceId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Get resource
            var resourceSpec = new BaseSpecification<LibraryResource>(lr => lr.ResourceId == resourceId);
            resourceSpec.EnableSplitQuery();
            resourceSpec.ApplyInclude(q => q.Include(r => r.LibraryResourceUrls));
            var resource = await _unitOfWork.Repository<LibraryResource, int>().GetWithSpecAsync(resourceSpec);
            if (resource is null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "resource" : "tài nguyên"));
            }

            if (!resource.FileFormat.ToLower().Equals("image"))
            {
                return new ServiceResult<Stream>(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            var pdfStream = await DownloadWithoutWaterMark(resource.ResourceUrl);
            return new ServiceResult<Stream>(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), pdfStream);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get digital resource preview");
        }
    }

    // Functions to process
    private async Task<MemoryStream> DownloadAndAddWatermark(string pdfUrl, string watermarkText, bool isPreview)
    {
        using HttpClient client = new HttpClient();
        byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("Downloaded PDF file is empty or corrupted.");
        }

        using MemoryStream inputPdfStream = new MemoryStream(pdfBytes);
        MemoryStream outputPdfStream = new MemoryStream();

        try
        {
            PdfReader reader = new PdfReader(inputPdfStream);
            int totalPages = reader.NumberOfPages;

            if (totalPages == 0)
            {
                throw new InvalidOperationException("The PDF file contains no pages.");
            }

            // if (pageNumber < 1 || pageNumber > totalPages)
            // {
            //     throw new ArgumentException($"Invalid page number {pageNumber}. Total pages: {totalPages}");
            // }

            Document document = new Document(reader.GetPageSizeWithRotation(1));
            PdfWriter writer = PdfWriter.GetInstance(document, outputPdfStream);
            document.Open();
            PdfContentByte contentByte = writer.DirectContent;

            int pagesToProcess = isPreview ? Math.Min(10, totalPages) : totalPages;
            for (int i = 1; i <= pagesToProcess; i++)
            {
                document.SetPageSize(reader.GetPageSizeWithRotation(i));
                document.NewPage();
                PdfImportedPage importedPage = writer.GetImportedPage(reader, i);
                contentByte.AddTemplate(importedPage, 0, 0);

                AddWatermark(contentByte, reader.GetPageSize(i), watermarkText);
            }

            document.Close();
            writer.Close();
            reader.Close();

            outputPdfStream.Position = 0;
            return outputPdfStream;
        }
        catch
        {
            outputPdfStream.Dispose();
            throw;
        }
    }

    private async Task<MemoryStream> DownloadWithoutWaterMark(string pdfUrl)
    {
        using HttpClient client = new HttpClient();
        byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new InvalidOperationException("Downloaded PDF file is empty or corrupted.");
        }

        using MemoryStream inputPdfStream = new MemoryStream(pdfBytes);
        MemoryStream outputPdfStream = new MemoryStream();

        try
        {
            PdfReader reader = new PdfReader(inputPdfStream);
            int totalPages = reader.NumberOfPages;

            if (totalPages == 0)
            {
                throw new InvalidOperationException("The PDF file contains no pages.");
            }

            Document document = new Document(reader.GetPageSizeWithRotation(1));
            PdfWriter writer = PdfWriter.GetInstance(document, outputPdfStream);
            document.Open();
            PdfContentByte contentByte = writer.DirectContent;

            int pagesToProcess = Math.Min(5, totalPages);
            for (int i = 1; i <= pagesToProcess; i++)
            {
                document.SetPageSize(reader.GetPageSizeWithRotation(i));
                document.NewPage();
                PdfImportedPage importedPage = writer.GetImportedPage(reader, i);
                contentByte.AddTemplate(importedPage, 0, 0);
            }

            document.Close();
            writer.Close();
            reader.Close();

            outputPdfStream.Position = 0;
            return outputPdfStream;
        }
        catch
        {
            outputPdfStream.Dispose();
            throw;
        }
    }

    private void AddWatermark(PdfContentByte contentByte, Rectangle pageSize, string watermarkText)
    {
        float fontSize = pageSize.Height * 0.05f;
        BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.EMBEDDED);

        contentByte.SaveState();
        contentByte.SetGState(new PdfGState { FillOpacity = 0.2f });
        contentByte.SetFontAndSize(baseFont, fontSize);
        contentByte.SetColorFill(BaseColor.Gray);


        float xPosition = pageSize.Width / 2;
        float yPosition = pageSize.Height / 2;

        float angle = 45;

        contentByte.BeginText();
        contentByte.ShowTextAligned(Element.ALIGN_CENTER, watermarkText, xPosition, yPosition, angle);
        contentByte.EndText();

        contentByte.RestoreState();
    }

    public async Task<IServiceResult<Stream>> AddAudioWatermark(int resourceId, string resourceLang, string email,
        int chunkSize)
    {
        var outputStream = new MemoryStream();
        var tts = await _voiceService.TextToVoiceFile(email);
        var ttsMemoryStream = (MemoryStream)tts.Data!;
        var ttsFile = new FormFile(ttsMemoryStream, 0, ttsMemoryStream.Length, "tts.mp3", "tts.mp3")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/pmeg"
        };
        var audioFile = await GetFullLargeFile(resourceId);
        var largeFile = new FormFile(audioFile.Data!, 0, audioFile.Data!.Length, "audio.mp3", "audio.mp3")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/pmeg"
        };

        using (var largeStream = largeFile.OpenReadStream())
        using (var smallStream = ttsFile.OpenReadStream())
        {
            smallStream.Position = 0;

            byte[] smallBuffer = new byte[smallStream.Length];
            int smallBytesRead = await smallStream.ReadAsync(smallBuffer, 0, smallBuffer.Length);
            await outputStream.WriteAsync(smallBuffer, 0, smallBytesRead);

            byte[] buffer = new byte[chunkSize];
            int bytesRead;

            while ((bytesRead = await largeStream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                // Write chunk from the large file
                await outputStream.WriteAsync(buffer, 0, bytesRead);

                // Write the full small stream (TTS)
                smallStream.Position = 0;
                await smallStream.CopyToAsync(outputStream);
            }
        }

        outputStream.Position = 0;

        return new ServiceResult<Stream>(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), outputStream);
    }

    private void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.Information(ex, "Failed to delete temporary file: {FilePath}", filePath);
        }
    }

    private static void InsertWatermark(WaveFileWriter writer, WaveFormatConversionStream conversionStream)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;

        // Reset watermark về đầu
        conversionStream.Position = 0;

        while ((bytesRead = conversionStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            writer.Write(buffer, 0, bytesRead);
        }

        // Đảm bảo dữ liệu được ghi
        writer.Flush();
    }

    private void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
    {
        if (begin.HasValue && end.HasValue && begin > end)
            throw new ArgumentOutOfRangeException(nameof(end), "end should be greater than begin");

        using (var reader = new Mp3FileReader(inputPath))
        using (var writer = File.Create(outputPath))
        {
            Mp3Frame frame;
            while ((frame = reader.ReadNextFrame()) != null)
            {
                if (reader.CurrentTime >= begin || !begin.HasValue)
                {
                    if (reader.CurrentTime <= end || !end.HasValue)
                        writer.Write(frame.RawData, 0, frame.RawData.Length);
                    else break;
                }
            }
        }
    }
}