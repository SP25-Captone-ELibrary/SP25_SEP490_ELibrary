using CloudinaryDotNet.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
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
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class LibraryShelfService : GenericService<LibraryShelf, LibraryShelfDto, int>,
    ILibraryShelfService<LibraryShelfDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemService<LibraryItemDto>> _libItemSvc;
    public LibraryShelfService(
        // Lazy services
        Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _libItemSvc = libItemSvc;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<LibraryShelf> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to LibraryShelfSpecification
            var shelfSpecification = specification as LibraryShelfSpecification;
            // Check if specification is null
            if (shelfSpecification == null)
            {
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Fail0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Count total library shelves
            var totalLibShelveWithSpec = await _unitOfWork.Repository<LibraryShelf, int>().CountAsync(shelfSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalLibShelveWithSpec / shelfSpecification.PageSize);

            // Set pagination to specification after count total library shelf
            if (shelfSpecification.PageIndex > totalPage
                || shelfSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                shelfSpecification.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            shelfSpecification.ApplyPaging(
                skip: shelfSpecification.PageSize * (shelfSpecification.PageIndex - 1),
                take: shelfSpecification.PageSize);
            
            // Retrieve all with spec
            var entities = await _unitOfWork.Repository<LibraryShelf, int>()
                .GetAllWithSpecAsync(shelfSpecification, tracked);
            if (entities.Any())
            {
                // Map to dto
                var dtoList = _mapper.Map<List<LibraryShelfDto>>(entities);
                
                // Map to get shelf detail
                var shelfDetailList = dtoList.Select(s => s.ToGetLibraryShelfDetailDto());
                
                // Pagination
                var paginationResult = new PaginatedResultDto<GetLibraryShelfDetailDto>(shelfDetailList,
                    shelfSpecification.PageIndex, shelfSpecification.PageSize, totalPage, totalLibShelveWithSpec);
                
                // Get data successfully
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: paginationResult);
            }

            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                data: new List<LibraryShelfDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all library shelf");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, LibraryShelfDto dto)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist library shelf by id
            var existingEntity = await _unitOfWork.Repository<LibraryShelf, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library shelf information" : "thông tin kệ"));
            }

            // Initialize custom errors
            var customErrs = new Dictionary<string, string[]>();

            // Build spec
            var spec = new BaseSpecification<LibraryItemInstance>(ls =>
                ls.LibraryItem.ShelfId == existingEntity.ShelfId &&
                ls.Status == nameof(LibraryItemInstanceStatus.InShelf));
            // Count existing items on shelf
            var onShelfItems = await _unitOfWork.Repository<LibraryItemInstance, int>().CountAsync(spec);

            // Check for classification number range modification
            if (!Equals(existingEntity.ClassificationNumberRangeFrom, dto.ClassificationNumberRangeFrom))
            {
                if (onShelfItems > 0)
                {
                    // Msg: Unable to update DDC range, as still existing {0} library items on shelf
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0032);
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        key: StringUtils.ToCamelCase(nameof(LibraryShelf.ClassificationNumberRangeFrom)),
                        msg: StringUtils.Format(errMsg, onShelfItems.ToString()));
                }
                
                // Assign update value
                existingEntity.ClassificationNumberRangeTo = dto.ClassificationNumberRangeTo;
            }

            if (!Equals(existingEntity.ClassificationNumberRangeTo, dto.ClassificationNumberRangeTo))
            {
                if (onShelfItems > 0)
                {
                    // Msg: Unable to update DDC range, as still existing {0} library items on shelf
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0032);
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        key: StringUtils.ToCamelCase(nameof(LibraryShelf.ClassificationNumberRangeTo)),
                        msg: StringUtils.Format(errMsg, onShelfItems.ToString()));
                }
                
                // Assign update value
                existingEntity.ClassificationNumberRangeFrom = dto.ClassificationNumberRangeTo;
            }

            // Check for shelf number modification
            if (!Equals(existingEntity.ShelfNumber, dto.ShelfNumber))
            {
                if (onShelfItems > 0)
                {
                    // Msg: Unable to update shelf number, as still existing {0} library items on shelf
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0033);
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        key: StringUtils.ToCamelCase(nameof(LibraryShelf.ShelfNumber)),
                        msg: StringUtils.Format(errMsg, onShelfItems.ToString()));
                }
                
                // Assign update value
                existingEntity.ShelfNumber = dto.ShelfNumber;
            }

            // Check for description modification
            if (!Equals(existingEntity.VieShelfName, dto.VieShelfName)) existingEntity.VieShelfName = dto.VieShelfName;
            if (!Equals(existingEntity.EngShelfName, dto.EngShelfName)) existingEntity.EngShelfName = dto.EngShelfName;

            // Check whether invokes any errors
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

            // Add modification date
            existingEntity.UpdateDate = currentLocalDateTime;

            if (!_unitOfWork.Repository<LibraryShelf, int>().HasChanges(existingEntity))
            {
                // Mark as update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }

            // Process save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Mark as update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }

            // Mark as update failed
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update library shelf");
        }
    }
    
    public async Task<IServiceResult> GetAllBySectionIdAsync(int sectionId)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryShelf>(lz => lz.SectionId == sectionId);
            var entities = await _unitOfWork.Repository<LibraryShelf, int>().GetAllWithSpecAsync(baseSpec);

            if (!entities.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                    _mapper.Map<IEnumerable<LibraryShelfDto>>(entities));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                _mapper.Map<IEnumerable<LibraryShelfDto>>(entities));
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }

    public async Task<IServiceResult> GetDetailAsync(int shelfId, ISpecification<LibraryItem> spec)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Try to retrieve shelf information by id
            var shelfEntity = await _unitOfWork.Repository<LibraryShelf, int>().GetByIdAsync(shelfId);
            if (shelfEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "library shelf" 
                        : "thông tin kệ sách"));
            }
            
            // Convert shelf to dto
            var shelfDto = _mapper.Map<LibraryShelfDto>(shelfEntity);
            
            // Try to parse specification to LibraryItemSpecification
            var itemSpecification = spec as LibraryItemSpecification;
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

            // Apply filter 
            itemSpecification.AddFilter(i => i.ShelfId == shelfId);
            
            // Retrieve data with spec and selector
            var libItemDtos = (await _libItemSvc.Value.GetAllWithSpecAndSelectorAsync(itemSpecification,
                selector: be => new LibraryItem
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
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances.Select(lia => new LibraryItemInstance()
                    {
                        LibraryItemInstanceId = lia.LibraryItemInstanceId,
                        LibraryItemId = lia.LibraryItemId,
                        Barcode = lia.Barcode,
                        Status = lia.Status,
                        CreatedAt = lia.CreatedAt,
                        UpdatedAt = lia.UpdatedAt,
                        CreatedBy = lia.CreatedBy,
                        UpdatedBy = lia.UpdatedBy,
                        IsDeleted = lia.IsDeleted,
                        IsCirculated = lia.IsCirculated,
                        BorrowRecordDetails = lia.BorrowRecordDetails.Select(brd => new BorrowRecordDetail()
                        {
                            BorrowRecordDetailId = brd.BorrowRecordDetailId,
                            BorrowRecordId = brd.BorrowRecordId,
                            LibraryItemInstanceId = brd.LibraryItemInstanceId,
                            ImagePublicIds = brd.ImagePublicIds,
                            Status = brd.Status,
                            ReturnDate = brd.ReturnDate,
                            DueDate = brd.DueDate,
                            ConditionId = brd.ConditionId,
                            ReturnConditionId = brd.ReturnConditionId,
                            ConditionCheckDate = brd.ConditionCheckDate,
                            IsReminderSent = brd.IsReminderSent,
                            TotalExtension = brd.TotalExtension,
                            BorrowRecord = new BorrowRecord()
                            {
                                BorrowRecordId = brd.BorrowRecord.BorrowRecordId,
                                BorrowRequestId = brd.BorrowRecord.BorrowRequestId,
                                LibraryCardId = brd.BorrowRecord.LibraryCardId,
                                BorrowDate = brd.BorrowRecord.BorrowDate,
                                BorrowType = brd.BorrowRecord.BorrowType,
                                SelfServiceBorrow = brd.BorrowRecord.SelfServiceBorrow,
                                SelfServiceReturn = brd.BorrowRecord.SelfServiceReturn,
                                TotalRecordItem = brd.BorrowRecord.TotalRecordItem,
                                ProcessedBy = brd.BorrowRecord.ProcessedBy
                            }
                        }).ToList(),
                        LibraryItemConditionHistories = lia.LibraryItemConditionHistories.Select(h => new LibraryItemConditionHistory()
                        {
                            ConditionHistoryId = h.ConditionHistoryId,
                            LibraryItemInstanceId = h.LibraryItemInstanceId,
                            ConditionId = h.ConditionId,
                            CreatedAt = h.CreatedAt,
                            UpdatedAt = h.UpdatedAt,
                            CreatedBy = h.CreatedBy,
                            UpdatedBy = h.UpdatedBy,
                            Condition = h.Condition,
                        }).ToList()
                    }).ToList(),
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList(),
                    BorrowRequestDetails = be.BorrowRequestDetails,
                    LibraryItemResources = be.LibraryItemResources
                })).Data as List<LibraryItem>;
            if (libItemDtos != null && libItemDtos.Any())
            {
                // Assign library items (if any)
                shelfDto.LibraryItems = _mapper.Map<List<LibraryItemDto>>(libItemDtos);
            }
            
            // Msg: Get data successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                // Convert to library shelf detail
                shelfDto.ToShelfDetailDto());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get shelf detail");
        }
    }

    public async Task<IServiceResult> GetDetailWithFloorZoneSectionByIdAsync(int shelfId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Initialize shelf base spec
            var shelfSpec = new BaseSpecification<LibraryShelf>(lf => lf.ShelfId == shelfId);
            // Apply including section
            shelfSpec.ApplyInclude(q => q
                .Include(s => s.Section)
                    .ThenInclude(s => s.Zone)
                        .ThenInclude(s => s.Floor)
            );
            
            // Retrieve all shelves with spec
            var existingEntity = await _unitOfWork.Repository<LibraryShelf, int>().GetWithSpecAsync(shelfSpec);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "shelf" : "kệ sách"));
            }
            
            // Map to dto
            var dto = _mapper.Map<LibraryShelfDto>(existingEntity);
            // Msg: Get data successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), dto.ToGetLibraryShelfDetailDto());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get detail with floor, zone and section");
        }
    }
    
    public async Task<IServiceResult> GetItemAppropriateShelfAsync(int libraryItemId,
        bool? isReferenceSection, bool? isChildrenSection, bool? isJournalSection, bool? isMostAppropriate)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Initialize valid DDC value
            decimal ddc;
            // Initialize not found message: Not found {0}
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            // Build library item spec 
            var libSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == libraryItemId);
            // Apply include 
            libSpec.ApplyInclude(q => q.Include(li => li.Category));
            // Retrieve data with spec
            var libItemDto = (await _libItemSvc.Value.GetWithSpecAsync(libSpec)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                    ? "item to seek out for appropriate shelf" 
                    : "tài liệu để tìm kiếm kệ phù hợp"));
            }
            else if (string.IsNullOrEmpty(libItemDto.ClassificationNumber))
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item's DDC to seek out for appropriate shelf" 
                        : "mã DDC của tài liệu để tìm kiếm kệ phù hợp"));
            }
            else if (!decimal.TryParse(libItemDto.ClassificationNumber, out ddc) || // Try parsed to decimal
                     !DeweyDecimalUtils.IsValidDeweyDecimal(libItemDto.ClassificationNumber)) // Check valid
            {
                // Invalid DDC 
                var invalidInputMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0001);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    invalidInputMsg + (isEng 
                        ? "DDC number is incorrect" 
                        : "Mã DDC không tồn tại"));
            }
            
            // Initialize shelf base spec
            var shelfSpec = new BaseSpecification<LibraryShelf>();
            // Apply including section
            shelfSpec.ApplyInclude(q => q
                .Include(s => s.Section)
                    .ThenInclude(s => s.Zone)
                        .ThenInclude(s => s.Floor)
            );
            
            // Initialize collection of shelves
            List<LibraryShelf> shelves;
            // Initialize invalid item message
            var invalidMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0017);
            // Determine library item category
            switch (libItemDto.Category.EnglishName)
            {
                case nameof(LibraryItemCategory.SingleBook) or nameof(LibraryItemCategory.BookSeries):
                    // Check for improper request
                    if (isJournalSection is true)
                    {
                        // Unable to place item {0} on shelf {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? $"'{nameof(LibraryLocation.Sections.MagazinesAndNews)}'" 
                                    : $"'{LibraryItemCategory.Magazine.GetDescription()} và {LibraryItemCategory.Newspaper.GetDescription()}'"));
                    }
                    else if (isReferenceSection is true)
                    {
                        // Unable to place item {0} on a shelf in section {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? $"'{nameof(LibraryLocation.Sections.Reference)}'" 
                                    : $"'{LibraryItemCategory.ReferenceBook.GetDescription()}'"));
                    }
                    
                    // Build-up filter for shelf spec
                    shelfSpec.AddFilter(
                        // All shelves belonging to the section that contains the ddc range
                        ls => ls.Section.ClassificationNumberRangeFrom <= ddc &&
                              ls.Section.ClassificationNumberRangeTo >= ddc &&
                              !ls.Section.IsJournalSection && // Is not journal section
                              !ls.Section.IsReferenceSection && // Is not reference section
                              ls.Section.IsChildrenSection == (isChildrenSection ?? false));// Include children section if request
                    break;
                case nameof(LibraryItemCategory.Newspaper) or nameof(LibraryItemCategory.Magazine):
                    if (isReferenceSection is true)
                    {
                        // Unable to place item {0} on a shelf in section {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? $"'{nameof(LibraryLocation.Sections.Reference)}'" 
                                    : $"'{LibraryItemCategory.ReferenceBook.GetDescription()}'"));
                    }
                    else if (isChildrenSection is true)
                    {
                        // Unable to place item {0} on a shelf in section {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? "'Children section'" 
                                    : "'Sách thiếu nhi'"));
                    }
                    
                    // Build-up filter for shelf spec
                    shelfSpec.AddFilter(
                        // All shelves belonging to the section that contains the ddc range
                        ls => ls.Section.ClassificationNumberRangeFrom <= ddc &&
                              ls.Section.ClassificationNumberRangeTo >= ddc &&
                              ls.Section.IsJournalSection && // Is journal section
                              !ls.Section.IsReferenceSection && // Is not reference section
                              !ls.Section.IsChildrenSection); // Is not children section
                    break;
                case nameof(LibraryItemCategory.ReferenceBook):
                    if (isJournalSection is true)
                    {
                        // Unable to place item {0} on shelf {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? $"'{nameof(LibraryLocation.Sections.MagazinesAndNews)}'" 
                                    : $"'{LibraryItemCategory.Magazine.GetDescription()} và {LibraryItemCategory.Newspaper.GetDescription()}'"));
                    }
                    else if (isChildrenSection is true)
                    {
                        // Unable to place item {0} on a shelf in section {1}
                        return new ServiceResult(ResultCodeConst.LibraryItem_Warning0017,
                            StringUtils.Format(invalidMsg,
                                isEng
                                    ? $"'{libItemDto.Category.EnglishName}'"
                                    : $"'{libItemDto.Category.VietnameseName}'",
                                isEng 
                                    ? "'Children section'" 
                                    : "'Sách thiếu nhi'"));
                    }
                    
                    // Build-up filter for shelf spec
                    shelfSpec.AddFilter(
                        // All shelves belonging to the section that contains the ddc range
                        ls => ls.Section.ClassificationNumberRangeFrom <= ddc &&
                              ls.Section.ClassificationNumberRangeTo >= ddc &&
                              !ls.Section.IsJournalSection && // Is not journal section
                              ls.Section.IsReferenceSection && // Is reference section
                              !ls.Section.IsChildrenSection); // Is not children section
                    break;
                default:
                    // In case create new category
                    // Get all shelves that DDC range belongs to. No matter reference, journal or children 
                    shelves = (await _unitOfWork.Repository<LibraryShelf, int>()
                        .GetAllWithSpecAsync(new BaseSpecification<LibraryShelf>( // All shelves
                            // Belong to the section that contains the ddc range
                            ls => ls.Section.ClassificationNumberRangeFrom <= ddc &&
                                        ls.Section.ClassificationNumberRangeTo >= ddc))
                        ).ToList();
                    break;
            }

            // Retrieve all shelves with spec
            shelves = (await _unitOfWork.Repository<LibraryShelf, int>().GetAllWithSpecAsync(shelfSpec)).ToList();
            if (shelves.Any())
            {
                // Initialize zone dto
                LibrarySectionDto? sectionDto = _mapper.Map<LibrarySectionDto>(shelves.First().Section);
                
                // Handle eliminate all shelves that not a range of item's DDC
                if (isMostAppropriate != null && isMostAppropriate == true)
                {
                    shelves = shelves.Where(s => 
                        s.ClassificationNumberRangeFrom <= ddc &&
                        s.ClassificationNumberRangeTo >= ddc).ToList();
                }
                else // Order by most appropriate range
                {
                    shelves = shelves
                        .OrderBy(shelf =>
                            // Check DDC value is within range
                            // Return 0 if it contains the DDC, otherwise return 1
                            (shelf.ClassificationNumberRangeFrom <= ddc && ddc <= shelf.ClassificationNumberRangeTo) ? 0 : 1)
                        .ThenBy(shelf =>
                        {
                            // Determine range contains the DDC
                            bool containsClassificationNum = shelf.ClassificationNumberRangeFrom <= ddc && ddc <= shelf.ClassificationNumberRangeTo;
                            if (containsClassificationNum)
                            {
                                // For shelves containing the DDC, calculate the range width
                                return shelf.ClassificationNumberRangeTo - shelf.ClassificationNumberRangeFrom;
                            }
                            else
                            {
                                // Calculate the minimum distance
                                var distanceFrom = Math.Abs(ddc - shelf.ClassificationNumberRangeFrom);
                                var distanceTo = Math.Abs(ddc - shelf.ClassificationNumberRangeTo);
                                return Math.Min(distanceFrom, distanceTo);
                            }
                        })
                        .ToList();
                }
                
                // Map to shelf dto
                var shelfDtoList = _mapper.Map<List<LibraryShelfDto>>(shelves);
                // Msg: Get data successfully   
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    // Convert to item appropriate shelf dto
                    shelfDtoList.ToItemAppropriateShelfDto(
                        itemClassificationNumber: libItemDto.ClassificationNumber,
                        section: sectionDto));
            }
            
            // Msg: No suitable shelf found for the requested item
            return new ServiceResult(ResultCodeConst.LibraryItem_Warning0018,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryItem_Warning0018));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get appropriate shelf for specified library item");
        }
    }
}