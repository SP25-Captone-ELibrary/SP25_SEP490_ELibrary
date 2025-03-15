using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
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
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRequestService : GenericService<BorrowRequest, BorrowRequestDto, int>,
    IBorrowRequestService<BorrowRequestDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecSvc;

    private readonly IEmailService _emailSvc;
    private readonly Lazy<IFineService<FineDto>> _fineSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventorySvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;
    
    private readonly BorrowSettings _borrowSettings;
    private readonly AppSettings _appSettings;

    public BorrowRequestService(
        // Lazy services
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        Lazy<IUserService<UserDto>> userSvc,
        Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecSvc,
        Lazy<IFineService<FineDto>> fineSvc,

        ILibraryCardService<LibraryCardDto> cardSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ILibraryItemInventoryService<LibraryItemInventoryDto> inventorySvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        IOptionsMonitor<BorrowSettings> monitor,
        IOptionsMonitor<AppSettings> monitor1,
        
        IEmailService emailSvc,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cardSvc = cardSvc;
        _userSvc = userSvc;
        _fineSvc = fineSvc;
        _emailSvc = emailSvc;
        _libItemSvc = libItemSvc;
        _inventorySvc = inventorySvc;
        _itemInstanceSvc = itemInstanceSvc;
        _borrowRecSvc = borrowRecSvc;
        _reservationQueueSvc = reservationQueueSvc;
        
        _borrowSettings = monitor.CurrentValue;
        _appSettings = monitor1.CurrentValue;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<BorrowRequest> spec,
        bool tracked = true)
    {
        try
        {
            // Check for proper specification
            var borrowReqSpec = spec as BorrowRequestSpecification;
            if (borrowReqSpec == null) // is null specification
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total borrow request
            var totalBorrowReqWithSpec = await _unitOfWork.Repository<BorrowRequest, int>().CountAsync(borrowReqSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalBorrowReqWithSpec / borrowReqSpec.PageSize);

            // Set pagination to specification after count total borrow req
            if (borrowReqSpec.PageIndex > totalPage
                || borrowReqSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                borrowReqSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            borrowReqSpec.ApplyPaging(
                skip: borrowReqSpec.PageSize * (borrowReqSpec.PageIndex - 1),
                take: borrowReqSpec.PageSize);

            // Get all with spec
            var entities = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAsync(borrowReqSpec, tracked: false);

            if (entities.Any()) // Exist data
            {
                // Convert to dto collection 
                var borrowReqDtos = _mapper.Map<List<BorrowRequestDto>>(entities);

                // Set null borrow request details
                borrowReqDtos.ForEach(br => br.BorrowRequestDetails = new List<BorrowRequestDetailDto>());

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<BorrowRequestDto>(borrowReqDtos,
                    borrowReqSpec.PageIndex, borrowReqSpec.PageSize, totalPage, totalBorrowReqWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto 
                _mapper.Map<IEnumerable<BorrowRequestDto>>(entities));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all borrow data");
        }
    }

    public async Task<IServiceResult> GetByIdAsync(int id, string? email, Guid? userId)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Add filter (if any)
            if (!string.IsNullOrWhiteSpace(email))
            {
                baseSpec.AddFilter(br => br.LibraryCard.Users.Any(u => u.Email == email));
            }
            if (userId.HasValue && userId != Guid.Empty)
            {
                baseSpec.AddFilter(br => br.LibraryCard.Users.Any(u => u.UserId == userId));
            }
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRequest()
                {
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    RequestDate = br.RequestDate,
                    ExpirationDate = br.ExpirationDate,
                    Status = br.Status,
                    Description = br.Description,
                    CancelledAt = br.CancelledAt,
                    CancellationReason = br.CancellationReason,
                    IsReminderSent = br.IsReminderSent,
                    TotalRequestItem = br.TotalRequestItem,
                    BorrowRequestDetails = br.BorrowRequestDetails.Select(brd => new BorrowRequestDetail()
                    {
                        BorrowRequestDetailId = brd.BorrowRequestDetailId,
                        BorrowRequestId = brd.BorrowRequestId,
                        LibraryItemId = brd.LibraryItemId,
                        LibraryItem = new LibraryItem()
                        {
                            LibraryItemId = brd.LibraryItem.LibraryItemId,
                            Title = brd.LibraryItem.Title,
                            SubTitle = brd.LibraryItem.SubTitle,
                            Responsibility = brd.LibraryItem.Responsibility,
                            Edition = brd.LibraryItem.Edition,
                            EditionNumber = brd.LibraryItem.EditionNumber,
                            Language = brd.LibraryItem.Language,
                            OriginLanguage = brd.LibraryItem.OriginLanguage,
                            Summary = brd.LibraryItem.Summary,
                            CoverImage = brd.LibraryItem.CoverImage,
                            PublicationYear = brd.LibraryItem.PublicationYear,
                            Publisher = brd.LibraryItem.Publisher,
                            PublicationPlace = brd.LibraryItem.PublicationPlace,
                            ClassificationNumber = brd.LibraryItem.ClassificationNumber,
                            CutterNumber = brd.LibraryItem.CutterNumber,
                            Isbn = brd.LibraryItem.Isbn,
                            Ean = brd.LibraryItem.Ean,
                            EstimatedPrice = brd.LibraryItem.EstimatedPrice,
                            PageCount = brd.LibraryItem.PageCount,
                            PhysicalDetails = brd.LibraryItem.PhysicalDetails,
                            Dimensions = brd.LibraryItem.Dimensions,
                            AccompanyingMaterial = brd.LibraryItem.AccompanyingMaterial,
                            Genres = brd.LibraryItem.Genres,
                            GeneralNote = brd.LibraryItem.GeneralNote,
                            BibliographicalNote = brd.LibraryItem.BibliographicalNote,
                            TopicalTerms = brd.LibraryItem.TopicalTerms,
                            AdditionalAuthors = brd.LibraryItem.AdditionalAuthors,
                            CategoryId = brd.LibraryItem.CategoryId,
                            ShelfId = brd.LibraryItem.ShelfId,
                            GroupId = brd.LibraryItem.GroupId,
                            Status = brd.LibraryItem.Status,
                            IsDeleted = brd.LibraryItem.IsDeleted,
                            IsTrained = brd.LibraryItem.IsTrained,
                            CanBorrow = brd.LibraryItem.CanBorrow,
                            TrainedAt = brd.LibraryItem.TrainedAt,
                            CreatedAt = brd.LibraryItem.CreatedAt,
                            UpdatedAt = brd.LibraryItem.UpdatedAt,
                            UpdatedBy = brd.LibraryItem.UpdatedBy,
                            CreatedBy = brd.LibraryItem.CreatedBy,
                            // References
                            Category = brd.LibraryItem.Category,
                            Shelf = brd.LibraryItem.Shelf,
                            LibraryItemInventory = brd.LibraryItem.LibraryItemInventory,
                            LibraryItemReviews = brd.LibraryItem.LibraryItemReviews,
                            LibraryItemAuthors = brd.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                            {
                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                LibraryItemId = ba.LibraryItemId,
                                AuthorId = ba.AuthorId,
                                Author = ba.Author
                            }).ToList()
                        },
                    }).ToList(),
                    ReservationQueues = br.ReservationQueues.Select(rq => new ReservationQueue()
                    {
                        QueueId = rq.QueueId,
                        LibraryItemId = rq.LibraryItemId,
                        LibraryItemInstanceId = rq.LibraryItemInstanceId,
                        LibraryCardId = rq.LibraryCardId,
                        QueueStatus = rq.QueueStatus,
                        BorrowRequestId = rq.BorrowRequestId,
                        IsReservedAfterRequestFailed = rq.IsReservedAfterRequestFailed,
                        ExpectedAvailableDateMin = rq.ExpectedAvailableDateMin,
                        ExpectedAvailableDateMax = rq.ExpectedAvailableDateMax,
                        ReservationDate = rq.ReservationDate,
                        ExpiryDate = rq.ExpiryDate,
                        IsNotified = rq.IsNotified,
                        CancelledBy = rq.CancelledBy,
                        CancellationReason = rq.CancellationReason,
                        LibraryItem = new LibraryItem()
                        {
                            LibraryItemId = rq.LibraryItem.LibraryItemId,
                            Title = rq.LibraryItem.Title,
                            SubTitle = rq.LibraryItem.SubTitle,
                            Responsibility = rq.LibraryItem.Responsibility,
                            Edition = rq.LibraryItem.Edition,
                            EditionNumber = rq.LibraryItem.EditionNumber,
                            Language = rq.LibraryItem.Language,
                            OriginLanguage = rq.LibraryItem.OriginLanguage,
                            Summary = rq.LibraryItem.Summary,
                            CoverImage = rq.LibraryItem.CoverImage,
                            PublicationYear = rq.LibraryItem.PublicationYear,
                            Publisher = rq.LibraryItem.Publisher,
                            PublicationPlace = rq.LibraryItem.PublicationPlace,
                            ClassificationNumber = rq.LibraryItem.ClassificationNumber,
                            CutterNumber = rq.LibraryItem.CutterNumber,
                            Isbn = rq.LibraryItem.Isbn,
                            Ean = rq.LibraryItem.Ean,
                            EstimatedPrice = rq.LibraryItem.EstimatedPrice,
                            PageCount = rq.LibraryItem.PageCount,
                            PhysicalDetails = rq.LibraryItem.PhysicalDetails,
                            Dimensions = rq.LibraryItem.Dimensions,
                            AccompanyingMaterial = rq.LibraryItem.AccompanyingMaterial,
                            Genres = rq.LibraryItem.Genres,
                            GeneralNote = rq.LibraryItem.GeneralNote,
                            BibliographicalNote = rq.LibraryItem.BibliographicalNote,
                            TopicalTerms = rq.LibraryItem.TopicalTerms,
                            AdditionalAuthors = rq.LibraryItem.AdditionalAuthors,
                            CategoryId = rq.LibraryItem.CategoryId,
                            ShelfId = rq.LibraryItem.ShelfId,
                            GroupId = rq.LibraryItem.GroupId,
                            Status = rq.LibraryItem.Status,
                            IsDeleted = rq.LibraryItem.IsDeleted,
                            IsTrained = rq.LibraryItem.IsTrained,
                            CanBorrow = rq.LibraryItem.CanBorrow,
                            TrainedAt = rq.LibraryItem.TrainedAt,
                            CreatedAt = rq.LibraryItem.CreatedAt,
                            UpdatedAt = rq.LibraryItem.UpdatedAt,
                            UpdatedBy = rq.LibraryItem.UpdatedBy,
                            CreatedBy = rq.LibraryItem.CreatedBy,
                            // References
                            Category = rq.LibraryItem.Category,
                            Shelf = rq.LibraryItem.Shelf,
                            LibraryItemInventory = rq.LibraryItem.LibraryItemInventory,
                            LibraryItemReviews = rq.LibraryItem.LibraryItemReviews,
                            LibraryItemAuthors = rq.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                            {
                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
                                LibraryItemId = ba.LibraryItemId,
                                AuthorId = ba.AuthorId,
                                Author = ba.Author
                            }).ToList()
                        },
                        LibraryItemInstance = rq.LibraryItemInstance
                    }).ToList()
                });
            if (existingEntity != null)
            {
                // Map to dto
                var dto = _mapper.Map<BorrowRequestDto>(existingEntity);
                // Convert to GetBorrowRequestDto
                var getBorrowReqDto = dto.ToGetBorrowRequestDto();
                    
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), getBorrowReqDto);
            }
            
            // Msg: Not found {0}
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "borrow request" : "lịch sử đăng ký mượn"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get borrow data by id");
        }
    }

    public async Task<IServiceResult> CreateAsync(string email, BorrowRequestDto dto, List<int> reservationItemIds)
    {
        try
        {
            // This func only process after calling func CheckUnavailableForBorrowRequestAsync in LibraryItemService
            // The reason why not compare total units due to user may remove item when confirm all items to borrow or reserve 
            
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
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access"); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;

            // Retrieve library card information
            var libCard = (await _cardSvc.GetByIdAsync(validCardId)).Data as LibraryCardDto;
            if (libCard == null)
            {
                // Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
            }

            // Count user's total pending fines 
            var totalPendingFine = (await _fineSvc.Value.CountAsync(
                new BaseSpecification<Fine>(f =>
                    f.Status == FineStatus.Pending &&
                    f.BorrowRecordDetail.BorrowRecord.LibraryCardId == validCardId))).Data;
            // Exist fine has not paid yet
            if (totalPendingFine != null && 
                int.TryParse(totalPendingFine.ToString(), out var pendingFine) && pendingFine > 0)
            {
                // Msg: Existing {0} fines haven't been paid yet. Please make payment to continue
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Fine_Warning0001);
                return new ServiceResult(ResultCodeConst.Fine_Warning0001,
                    StringUtils.Format(errMsg, pendingFine.ToString()));
            }
            
            // Check exist any details 
            if (dto.BorrowRequestDetails.Any())
            {
                // Validate borrow amount before handling each request detail 
                var validateAmountRes = await ValidateBorrowAmountAsync(
                    totalItem: dto.BorrowRequestDetails.Count,
                    libraryCardId: libCard.LibraryCardId);
                if (validateAmountRes != null) return validateAmountRes;
            }

            // Custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Initialize unique detail check
            var uniqueDetailSet = new HashSet<int>();

            // Convert to list 
            var detailList = dto.BorrowRequestDetails.ToList();
            // Initialize borrow details
            var borrowDetails = new List<BorrowRequestDetailDto>();
            // Iterate each of detail to check for quantity availability
            for (int i = 0; i < detailList.Count; ++i)
            {
                var detail = detailList[i];

                // Check exist item 
                var isItemExist =
                    (await _libItemSvc.AnyAsync(li => li.LibraryItemId == detail.LibraryItemId)).Data is true;
                if (!isItemExist)
                {
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        key: $"libraryItemIds[{i}]",
                        msg: StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                            isEng ? "item" : "tài liệu"));
                }
                else
                {
                    // Check duplicate
                    if (!uniqueDetailSet.Add(detail.LibraryItemId))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // Duplicate items are not allowed. You can only borrow one copy of each item per time
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0003));
                    }

                    // Check already requested the item 
                    var isAlreadyRequested = await _unitOfWork.Repository<BorrowRequest, int>()
                        .AnyAsync(br =>
                                br.Status == BorrowRequestStatus.Created && // Elements with expired status
                                br.LibraryCardId == libCard.LibraryCardId && // With specific library card 
                                br.BorrowRequestDetails.Any(brd =>
                                    brd.LibraryItemId == detail.LibraryItemId) // With specific item
                        );
                    // Check already borrowed the item
                    var isAlreadyBorrowed = (await _borrowRecSvc.Value.AnyAsync(
                        br => br.LibraryCardId == validCardId && 
                              br.BorrowRecordDetails.Any(brd =>
                                brd.Status != BorrowRecordStatus.Returned &&
                                brd.LibraryItemInstance.LibraryItemId == detail.LibraryItemId))).Data is true;
                    if (isAlreadyRequested || isAlreadyBorrowed)
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // The item is currently borrowed and cannot be borrowed again
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0004));
                    }
                    
                    // Check already reserved the item
                    var isAlreadyReserved = (await _reservationQueueSvc.AnyAsync(r => 
                        r.LibraryItemId == detail.LibraryItemId &&
                        r.LibraryCardId == validCardId &&
                        r.QueueStatus != ReservationQueueStatus.Expired &&
                        r.QueueStatus != ReservationQueueStatus.Cancelled &&
                        r.QueueStatus != ReservationQueueStatus.Collected)).Data is true;
                    if (isAlreadyReserved)
                    {
                        // Msg: You have already reserved item {0}
                        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0004);
                        
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            msg: StringUtils.Format(errMsg, string.Empty));
                    }

                    // Retrieving item inventory 
                    var itemIven =
                        (await _inventorySvc.GetByIdAsync(id: detail.LibraryItemId)).Data as LibraryItemInventoryDto;
                    if (itemIven == null)
                    {
                        // Unknown error
                        return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                    }

                    // Check available item units
                    if (itemIven.AvailableUnits == 0)
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // Item quantity is not available
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0002));
                    }

                    // Add borrow detail
                    borrowDetails.Add(new()
                    {
                        LibraryItemId = detail.LibraryItemId
                    });

                    // Update inventory total
                    if (itemIven.AvailableUnits > 0)
                    {
                        // Reduce available units
                        itemIven.AvailableUnits--;
                        // Increase request units
                        itemIven.RequestUnits++;
                    }

                    // Process update without save change
                    await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
                }
            }

            // Check if any error invoke
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

            // Initialize amount calculation props
            var totalRequestAmount = 0;
            var totalBorrowingAmount = 0;
            
            // Build spec
            var borrowReqSpec = new BaseSpecification<BorrowRequest>(br =>
                br.LibraryCardId == validCardId && // with specific library card
                br.Status == BorrowRequestStatus.Created); // In created status
            // Count requesting amount to check for threshold
            var listRequestAmount = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAndSelectorAsync(borrowReqSpec, selector: s => s.BorrowRequestDetails.Count);
            totalRequestAmount = listRequestAmount?.Select(i => i).Sum() ?? 0;
            
            // Build spec
            var borrowRecSpec = new BaseSpecification<BorrowRecord>(br => br.LibraryCardId == validCardId); 
            // Count borrowed amount to check for threshold
            var listBorrowingAmount = (await _borrowRecSvc.Value
                    .GetAllWithSpecAndSelectorAsync(borrowRecSpec, 
                        selector: s => s.BorrowRecordDetails
                            .Count(brd => brd.Status == BorrowRecordStatus.Borrowing && // Is borrowing 
                                          brd.ReturnDate == null)) // Has not return yet
                ).Data as List<int>;
            totalBorrowingAmount = listBorrowingAmount?.Select(i => i).Sum() ?? 0;
            
            // Check whether request + borrowed amount is exceed than borrow threshold 
            if(totalBorrowingAmount > 0 || totalRequestAmount > 0)
            {
                // Sum requested + borrowed + item to borrow 
                var sumTotal = totalRequestAmount + totalBorrowingAmount + dto.BorrowRequestDetails.Count; 
                // Validate borrow amount
                var validateAmountRes = await ValidateBorrowAmountAsync(
                    totalItem: sumTotal,
                    libraryCardId: validCardId);
                if (validateAmountRes != null) return validateAmountRes;
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Create reservations (if any)
            // Initialize reservation queue collection
            var reservationQueues = new List<ReservationQueueDto>();
            // Iterate each reservation to check whether user has already reserved
            foreach (var libItemId in reservationItemIds)
            {
                var allowToReserveRes = (await _reservationQueueSvc.CheckAllowToReserveByItemIdAsync(
                    itemId: libItemId,
                    email: userDto.Email)).Data is true;
                if (allowToReserveRes) // Is allow to reserve
                {
                    // Add new reservation                    
                    reservationQueues.Add(new ReservationQueueDto()
                    {
                        LibraryItemId = libItemId,
                        IsReservedAfterRequestFailed = true
                    });
                }
            }
            // Add range without save changes
            var createRes = await _reservationQueueSvc.CreateRangeWithoutSaveChangesAsync(validCardId, reservationQueues.ToList());
            // Failed to create
            if (createRes.ResultCode != ResultCodeConst.SYS_Success0001) return createRes; 
            
            // Try to map data response after create reservations
            reservationQueues = createRes.Data as List<ReservationQueueDto>;
            
            // Progress create request
            var borrowReqDto = new BorrowRequestDto()
            {
                LibraryCardId = validCardId,
                // Assign borrow request details (if any)
                BorrowRequestDetails = borrowDetails.Any() 
                    ? borrowDetails.ToList() 
                    : new List<BorrowRequestDetailDto>(),
                // Assign reservation queues (if any)
                ReservationQueues = reservationQueues != null && reservationQueues.Any() 
                    ? reservationQueues.ToList() 
                    : new List<ReservationQueueDto>(),
                TotalRequestItem = borrowDetails.Count,
                RequestDate = currentLocalDateTime,
                ExpirationDate = currentLocalDateTime.AddDays(_borrowSettings.BorrowRequestExpirationInDays),
                Status = BorrowRequestStatus.Created,
                IsReminderSent = false,
            };
            // Process add borrow request
            await _unitOfWork.Repository<BorrowRequest, int>().AddAsync(_mapper.Map<BorrowRequest>(borrowReqDto));
            
            // Update library item borrow more status if it true in current request
            if (libCard.IsAllowBorrowMore)
            {
                await _cardSvc.UpdateBorrowMoreStatusWithoutSaveChangesAsync(validCardId);
            }

            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                if (reservationQueues != null && reservationQueues.Any())
                {
                    // Try to include library item for mapped reservation    
                    foreach (var reservation in reservationQueues)
                    {
                        reservation.LibraryItem = (await _libItemSvc.GetByIdAsync(reservation.LibraryItemId)).Data as LibraryItemDto ?? null!;
                    }
                }
                
                // Try to retrieve library item for each borrow request details
                if (borrowReqDto.BorrowRequestDetails.Count > 0)
                {
                    foreach (var borrowRec in borrowReqDto.BorrowRequestDetails)
                    {
                        borrowRec.LibraryItem = (await _libItemSvc.GetByIdAsync(borrowRec.LibraryItemId)).Data as LibraryItemDto ?? null!;
                    }
                }
                
                // Process send email
                await SendBorrowRequestSuccessEmailAsync(
                    user: userDto,
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact,
                    borrowReq: borrowReqDto,
                    reservationQueues: reservationQueues);
                
                // Custom message
                var customMsg = borrowReqDto.BorrowRequestDetails.Any()
                    ? isEng 
                        ? $"Total {detailList.Count.ToString()} items request to borrow"
                        : $"Tổng {detailList.Count.ToString()} tài liệu yêu cầu mượn"
                    : string.Empty;
                var customMsgA = reservationQueues != null && reservationQueues.Any()
                    ? isEng
                        ? $"{reservationQueues.Count.ToString()} items reserved" 
                        : $"{reservationQueues.Count.ToString()} tài liệu được đặt mượn"
                    : string.Empty;
                
                // Check for different cases
                if (!string.IsNullOrEmpty(customMsg) && !string.IsNullOrEmpty(customMsgA))
                {
                    return new ServiceResult(ResultCodeConst.Borrow_Success0001, 
                        isEng 
                            ? $"{customMsg} and {customMsgA}" 
                            : $"{customMsg} và {customMsgA}");
                }
                if (!string.IsNullOrEmpty(customMsg))
                {
                    // Total {0} item(s) have been borrowed successfully
                    return new ServiceResult(ResultCodeConst.Borrow_Success0001,
                        StringUtils.Format(
                            await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0001),
                            detailList.Count.ToString()));
                }
                if (!string.IsNullOrEmpty(customMsgA))
                {
                    return new ServiceResult(ResultCodeConst.Borrow_Success0001, customMsgA);
                }
            }

            // An error occurred, the item borrowing registration failed
            return new ServiceResult(ResultCodeConst.Borrow_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0001));
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
            throw new Exception("Error invoke when process create borrow request from public");
        }
    }

    public async Task<IServiceResult> AddItemAsync(string email, int id, int libraryItemId)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access"); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include request details
                .Include(br => br.BorrowRequestDetails)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check whether request library card is correct
            if (!Equals(existingEntity.LibraryCardId, validCardId))
            {
                // Cannot process as library card is incorrect
                return new ServiceResult(ResultCodeConst.Borrow_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0008));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Check exist item 
            var libItemDto =
                (await _libItemSvc.GetByIdAsync(libraryItemId)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"));
            }
            
            // Check whether item has already existed in request details
            if (existingEntity.BorrowRequestDetails.Any(li => li.LibraryItemId == libraryItemId))
            {
                // Msg: Failed to add item to borrow request as {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0022);
                return new ServiceResult(ResultCodeConst.Borrow_Warning0022,
                    StringUtils.Format(errMsg, isEng 
                        ? "this item has already existed in borrow request" 
                        : "tài liệu đã tồn tại trong yêu cầu mượn"));
            }
            
            // Check already requested the item 
            var isAlreadyRequested = await _unitOfWork.Repository<BorrowRequest, int>()
                .AnyAsync(br =>
                        br.Status == BorrowRequestStatus.Created && // Elements with expired status
                        br.LibraryCardId == userDto.LibraryCardId && // With specific library card 
                        br.BorrowRequestDetails.Any(brd => brd.LibraryItemId == libraryItemId) // With specific item
                );
            // Check already borrowed the item
            var isAlreadyBorrowedItem = (await _borrowRecSvc.Value.AnyAsync(
                br => br.LibraryCardId == validCardId && 
                      br.BorrowRecordDetails.Any(brd =>
                          brd.Status != BorrowRecordStatus.Returned &&
                          brd.LibraryItemInstance.LibraryItemId == libraryItemId))).Data is true;
                    
            if (isAlreadyRequested || isAlreadyBorrowedItem)
            {
                // Msg: The item is currently borrowed and cannot be borrowed again
                return new ServiceResult(ResultCodeConst.Borrow_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0004));
            }
            
            // Check already reserved the item
            var isAlreadyReserved = (await _reservationQueueSvc.AnyAsync(r => 
                r.LibraryItemId == libraryItemId &&
                r.LibraryCardId == validCardId &&
                r.QueueStatus != ReservationQueueStatus.Expired &&
                r.QueueStatus != ReservationQueueStatus.Cancelled &&
                r.QueueStatus != ReservationQueueStatus.Collected)).Data is true;
            if (isAlreadyReserved)
            {
                // Msg: You have already reserved item {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0004);
                return new ServiceResult(ResultCodeConst.Reservation_Warning0004,
                    StringUtils.Format(errMsg, $"'{libItemDto.Title}'"));
            }
            
            // Retrieving item inventory 
            var itemIven =
                (await _inventorySvc.GetByIdAsync(id: libraryItemId)).Data as LibraryItemInventoryDto;
            if (itemIven == null)
            {
                // Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
            }

            // Check available item units
            if (itemIven.AvailableUnits == 0)
            {
                // Msg: Item quantity is not available
                return new ServiceResult(ResultCodeConst.Borrow_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0002));
            }
            
            // Update inventory total
            if (itemIven.AvailableUnits > 0)
            {
                // Reduce available units
                itemIven.AvailableUnits--;
                // Increase request units
                itemIven.RequestUnits++;
            }

            // Build spec
            var borrowReqSpec = new BaseSpecification<BorrowRequest>(br =>
                br.LibraryCardId == validCardId && // with specific library card
                br.Status == BorrowRequestStatus.Created); // In created status
            // Count requesting amount to check for threshold
            var listRequestAmount = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAndSelectorAsync(borrowReqSpec, selector: s => s.BorrowRequestDetails.Count);
            var totalRequestAmount = listRequestAmount?.Select(i => i).Sum() ?? 0;
            
            // Build spec
            var borrowRecSpec = new BaseSpecification<BorrowRecord>(br => br.LibraryCardId == validCardId); 
            // Count borrowed amount to check for threshold
            var listBorrowingAmount = (await _borrowRecSvc.Value
                        .GetAllWithSpecAndSelectorAsync(borrowRecSpec, 
                            selector: s => s.BorrowRecordDetails
                                .Count(brd => brd.Status == BorrowRecordStatus.Borrowing && // Is borrowing 
                                              brd.ReturnDate == null)) // Has not return yet
                ).Data as List<int>;
            var totalBorrowingAmount = listBorrowingAmount?.Select(i => i).Sum() ?? 0;
            
            // Check whether request + borrowed amount is exceed than borrow threshold 
            if(totalBorrowingAmount > 0 || totalRequestAmount > 0)
            {
                // Sum requested + borrowed + 1 more item to borrow
                var sumTotal = totalRequestAmount + totalBorrowingAmount + 1; 
                // Validate borrow amount
                var validateAmountRes = await ValidateBorrowAmountAsync(
                    totalItem: sumTotal,
                    libraryCardId: validCardId);
                if (validateAmountRes != null) return validateAmountRes;
            }
            
            // Process update without save change
            await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
            
            // Process add library item to request details
            existingEntity.BorrowRequestDetails.Add(new ()
            {
                LibraryItemId = libraryItemId
            });
            // Increase total units
            existingEntity.TotalRequestItem++;
            
            // Process update
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            // Saved DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Add item to borrow request successfully
                return new ServiceResult(ResultCodeConst.Borrow_Success0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0007));
            }
            
            // Msg:Failed to add item to borrow request
            return new ServiceResult(ResultCodeConst.Borrow_Fail0008,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0008));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add library item to borrow request");
        }
    }
    
    public async Task<IServiceResult> CancelAsync(string email, int id,
        string? cancellationReason, bool isConfirmed = false)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access"); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include reservations (if any)
                .Include(br => br.ReservationQueues)
                // Include request details
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check whether request library card is correct
            if (!Equals(existingEntity.LibraryCardId, validCardId))
            {
                // Cannot process as library card is incorrect
                return new ServiceResult(ResultCodeConst.Borrow_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0008));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Check whether request already cancelled
            if (existingEntity.Status == BorrowRequestStatus.Cancelled)
            {
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    isEng
                        ? "Cannot process as borrow request has been already cancelled"
                        : "Không thể hủy vì yêu cầu mượn đang ở trạng thái hủy");
            }
            
            // Check whether user confirmed or not 
            if (!isConfirmed) // Not confirm yet
            {
                var customMsg = isEng 
                    ? "Please confirm request details to process cancel" 
                    : "Vui lòng xác nhận số lượng trong yêu cầu mượn để tiến hành hủy";
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    message: customMsg,
                    data: existingEntity.BorrowRequestDetails.Select(brd => brd.LibraryItem).ToList());
            }
            
            // Iterate each of detail to update for quantity availability
            foreach (var detail in existingEntity.BorrowRequestDetails)
            {
                // Retrieving item inventory 
                var itemIven =
                    (await _inventorySvc.GetByIdAsync(id: detail.LibraryItemId)).Data as LibraryItemInventoryDto;
                if (itemIven == null)
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
                
                // Updated inventory units
                if (itemIven.RequestUnits > 0)
                {
                    // Request units
                    itemIven.RequestUnits--;
                    // Available units
                    itemIven.AvailableUnits++;
                }
                // Process update without save change
                await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Add cancellation props 
            existingEntity.Status = BorrowRequestStatus.Cancelled;
            existingEntity.CancelledAt = currentLocalDateTime;
            existingEntity.CancellationReason = cancellationReason;
            
            // Process update
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Cancel borrowing {0} item(s) successfully
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0002);
                return new ServiceResult(ResultCodeConst.Borrow_Success0002,
                    StringUtils.Format(msg, existingEntity.BorrowRequestDetails.Count.ToString()), true);
            }
            
            // Msg: Failed to cancel borrow request
            return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
        }
        catch (ForbiddenException)
        {
            throw;
        }    
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel borrow request");
        }
    }

    public async Task<IServiceResult> CancelManagementAsync(Guid libraryCardId, int id, 
        string? cancellationReason, bool isConfirmed = false)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.LibraryCardId, libraryCardId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
            }
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include reservations (if any)
                .Include(br => br.ReservationQueues)
                // Include request details
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Check whether request already cancelled
            if (existingEntity.Status == BorrowRequestStatus.Cancelled)
            {
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    isEng
                        ? "Cannot process as borrow request has been already cancelled"
                        : "Không thể hủy vì yêu cầu mượn đang ở trạng thái hủy");
            }
            
            // Check whether user confirmed or not 
            if (!isConfirmed) // Not confirm yet
            {
                var customMsg = isEng 
                    ? "Please confirm request details to process cancel" 
                    : "Vui lòng xác nhận số lượng trong yêu cầu mượn để tiến hành hủy";
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    message: customMsg,
                    data: existingEntity.BorrowRequestDetails.Select(brd => brd.LibraryItem).ToList());
            }
            
            // Iterate each of detail to update for quantity availability
            foreach (var detail in existingEntity.BorrowRequestDetails)
            {
                // Retrieving item inventory 
                var itemIven =
                    (await _inventorySvc.GetByIdAsync(id: detail.LibraryItemId)).Data as LibraryItemInventoryDto;
                if (itemIven == null)
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
                
                // Updated inventory units
                if (itemIven.RequestUnits > 0)
                {
                    // Request units
                    itemIven.RequestUnits--;
                    // Available units
                    itemIven.AvailableUnits++;
                }
                 // Process update without save change
                await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Add cancellation props 
            existingEntity.Status = BorrowRequestStatus.Cancelled;
            existingEntity.CancelledAt = currentLocalDateTime;
            existingEntity.CancellationReason = cancellationReason;
            
            // Process update
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Cancel borrowing {0} item(s) successfully
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0002);
                return new ServiceResult(ResultCodeConst.Borrow_Success0002,
                    StringUtils.Format(msg, existingEntity.BorrowRequestDetails.Count.ToString()), true);
            }
            
            // Msg: Failed to cancel borrow request
            return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel borrow request");
        }
    }
    
    public async Task<IServiceResult> CancelSpecificItemAsync(string email, int id, int libraryItemId)
    {
        try
        {   
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access"); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include reservations (if any)
                .Include(br => br.ReservationQueues)
                // Include request details
                .Include(br => br.BorrowRequestDetails)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check whether request library card is correct
            if (!Equals(existingEntity.LibraryCardId, validCardId))
            {
                // Cannot process as library card is incorrect
                return new ServiceResult(ResultCodeConst.Borrow_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0008));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Check whether request already cancelled
            if (existingEntity.Status == BorrowRequestStatus.Cancelled)
            {
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    isEng
                        ? "Cannot process as borrow request has been already cancelled"
                        : "Không thể hủy vì yêu cầu mượn đang ở trạng thái hủy");
            }
            
            // Check whether borrow request exist only one item
            if (existingEntity.BorrowRequestDetails.Count == 1)
            {
                // Is the last item
                if (existingEntity.BorrowRequestDetails.Any(brd => brd.LibraryItemId == libraryItemId))
                {
                    // Try to cancel borrow request
                    return await CancelAsync(email: email, id: id, cancellationReason: string.Empty, isConfirmed: true);
                }
                
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
            }
            
            // Try to retrieve specific borrow request detail based on item id
            var requestDetail = existingEntity.BorrowRequestDetails.FirstOrDefault(brd => brd.LibraryItemId == libraryItemId);
            if(requestDetail == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item in borrow request" 
                        : "tài liệu trong yêu cầu mượn"));
            }
            else
            {
                // Process cancel specific item in borrow request
                existingEntity.BorrowRequestDetails.Remove(requestDetail);
                // Reduce total borrow units
                existingEntity.TotalRequestItem--;
                // Process update entity
                await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
                
                // Update inventory
                // Retrieving item inventory 
                var itemIven =
                    (await _inventorySvc.GetByIdAsync(id: libraryItemId)).Data as LibraryItemInventoryDto;
                if (itemIven == null)
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
                
                // Updated inventory units
                if (itemIven.RequestUnits > 0)
                {
                    // Request units
                    itemIven.RequestUnits--;
                    // Available units
                    itemIven.AvailableUnits++;
                }
                // Process update without save change
                await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
                
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
                if (isSaved)
                {
                    // Cancel borrowing {0} item(s) successfully
                    var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0002);
                    return new ServiceResult(ResultCodeConst.Borrow_Success0002,
                        StringUtils.Format(msg, "1"), true);
                }
            }
            
            // Msg: Failed to cancel borrow request
            return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel specific item");
        }
    }
    
    public async Task<IServiceResult> CancelSpecificItemManagementAsync(Guid libraryCardId, int id, int libraryItemId)
    {
        try
        {   
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.LibraryCardId, libraryCardId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
            }
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include reservations (if any)
                .Include(br => br.ReservationQueues)
                // Include request details
                .Include(br => br.BorrowRequestDetails)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Check whether request already cancelled
            if (existingEntity.Status == BorrowRequestStatus.Cancelled)
            {
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    isEng
                        ? "Cannot process as borrow request has been already cancelled"
                        : "Không thể hủy vì yêu cầu mượn đang ở trạng thái hủy");
            }
            
            // Check whether borrow request exist only one item
            if (existingEntity.BorrowRequestDetails.Count == 1)
            {
                // Is the last item
                if (existingEntity.BorrowRequestDetails.Any(brd => brd.LibraryItemId == libraryItemId))
                {
                    // Try to cancel borrow request
                    return await CancelAsync(email: userDto.Email, id: id, cancellationReason: string.Empty, isConfirmed: true);
                }
                
                // Mark as fail to cancel
                return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
            }
            
            // Try to retrieve specific borrow request detail based on item id
            var requestDetail = existingEntity.BorrowRequestDetails.FirstOrDefault(brd => brd.LibraryItemId == libraryItemId);
            if(requestDetail == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "item in borrow request" 
                        : "tài liệu trong yêu cầu mượn"));
            }
            else
            {
                // Process cancel specific item in borrow request
                existingEntity.BorrowRequestDetails.Remove(requestDetail);
                // Reduce total borrow units
                existingEntity.TotalRequestItem--;
                // Process update entity
                await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
                
                // Update inventory
                // Retrieving item inventory 
                var itemIven =
                    (await _inventorySvc.GetByIdAsync(id: libraryItemId)).Data as LibraryItemInventoryDto;
                if (itemIven == null)
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
                
                // Updated inventory units
                if (itemIven.RequestUnits > 0)
                {
                    // Request units
                    itemIven.RequestUnits--;
                    // Available units
                    itemIven.AvailableUnits++;
                }
                // Process update without save change
                await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
                
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
                if (isSaved)
                {
                    // Cancel borrowing {0} item(s) successfully
                    var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0002);
                    return new ServiceResult(ResultCodeConst.Borrow_Success0002,
                        StringUtils.Format(msg, "1"), true);
                }
            }
            
            // Msg: Failed to cancel borrow request
            return new ServiceResult(ResultCodeConst.Borrow_Fail0009,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0009), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel specific item");
        }
    }
    
    public async Task<IServiceResult> UpdateStatusWithoutSaveChangesAsync(int id, BorrowRequestStatus status)
    {
        try
        {
            // Check exist borrow request
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }
            
            // Check for status change
            if (Equals(existingEntity.Status, status))
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            
            // Update prop
            existingEntity.Status = status;
            
            // Process update without save changes
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            
            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update borrow request status without save changes");
        }
    }

    public async Task<IServiceResult> CheckExistBarcodeInRequestAsync(int id, string barcode)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            var libItemDto = (await _libItemSvc.GetByInstanceBarcodeAsync(barcode)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "item instance by barcode" : "tài liệu"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<BorrowRequest>(br =>
                br.BorrowRequestId == id &&
                br.BorrowRequestDetails.Any(
                    brd => brd.LibraryItemId == libItemDto.LibraryItemId));
            // Check exist
            var isExistInRequest = await _unitOfWork.Repository<BorrowRequest, int>().AnyAsync(baseSpec);
            if (!isExistInRequest)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? $"item match code '{barcode}' in registered borrow request"
                        : $"tài liệu với số ĐKCB '{barcode}' trong yêu cầu đăng ký mượn"), false);
            }
            
            // Is exist -> Return library item instance  
            return await _itemInstanceSvc.Value.GetByBarcodeAsync(barcode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when check exist barcode in request");
        }
    }
    
    public async Task<IServiceResult?> ValidateBorrowAmountAsync(int totalItem, Guid libraryCardId)
    {
        // Determine current lang context
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        // Retrieve card by id 
        var libCardDto = (await _cardSvc.GetByIdAsync(libraryCardId)).Data as LibraryCardDto;
        if (libCardDto == null)
        {
            // Not found {0}
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
        }
        
        // Max amount to borrow (if any)
        var maxAmountToBorrow = libCardDto.MaxItemOnceTime;
        // Default Threshold amount
        var defaultThresholdTotal = _borrowSettings.BorrowAmountOnceTime;
        
        // Check for default amount boundary
        if (totalItem > defaultThresholdTotal) // Total item borrow exceed than default max amount to borrow
        {
            // Msg: You can borrow up to {0} items at a time
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0005);
            
            if (!libCardDto.IsAllowBorrowMore) // Is not allow to borrow more
            {
                return new ServiceResult(ResultCodeConst.Borrow_Warning0005, StringUtils.Format(msg, defaultThresholdTotal.ToString()));
            }

            if (libCardDto.IsAllowBorrowMore && // Is allow to borrow more
                maxAmountToBorrow > 0 && totalItem > maxAmountToBorrow) // Total item borrow not exceed max amount to borrow from lib card
            {
                return new ServiceResult(ResultCodeConst.Borrow_Warning0005, StringUtils.Format(msg, maxAmountToBorrow.ToString()));
            }
        }

        return null;
    }
    
    private async Task<bool> SendBorrowRequestSuccessEmailAsync(
        UserDto user,
        string libName, string libContact,
        BorrowRequestDto borrowReq, List<ReservationQueueDto>? reservationQueues = null)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Thông báo yêu cầu mượn tài liệu thành công";
                            
            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { user.Email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetBorrowRequestSuccessEmailBody(
                    user: user,
                    borrowReq: borrowReq,
                    reservationQueues: reservationQueues,
                    libName: libName,
                    libContact:libContact)
            );
                            
            // Process send email
            return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send library card activated email");
        }
    }

    private string GetBorrowRequestSuccessEmailBody(
        UserDto user,
        string libName, string libContact,
        BorrowRequestDto borrowReq, List<ReservationQueueDto>? reservationQueues = null)
    {
        // Initialize all main content of email
        string headerMessage = string.Empty;
        string mainMessage = string.Empty;
        string borrowSection = string.Empty;
        string reservationSection = string.Empty;
        
        if (borrowReq.BorrowRequestDetails.Any()) // Only process borrow when exist
        {
            headerMessage = "Yêu Cầu Mượn Sách Thành Công";
            mainMessage = "Yêu cầu mượn sách của bạn đã được xử lý thành công. Dưới đây là chi tiết yêu cầu mượn:";
            
            var borrowItemList = string.Join("", borrowReq.BorrowRequestDetails.Select(detail => 
                $"""
                 <li>
                     <p><strong>Tiêu đề:</strong> <span class="title">{detail.LibraryItem.Title}</span></p>
                     <p><strong>ISBN:</strong> <span class="isbn">{detail.LibraryItem.Isbn}</span></p>
                     <p><strong>Năm Xuất Bản:</strong> {detail.LibraryItem.PublicationYear}</p>
                     <p><strong>Nhà Xuất Bản:</strong> {detail.LibraryItem.Publisher}</p>
                 </li>
                 """));

            borrowSection = $$"""
                  <p><strong>Thông Tin Yêu Cầu Mượn:</strong></p>
                  <div class="details">
                      <ul>
                          <li><strong>Ngày Yêu Cầu:</strong> {{borrowReq.RequestDate:dd/MM/yyyy HH:mm}}</li>
                          <li><strong>Ngày Hết Hạn:</strong> {{borrowReq.ExpirationDate:dd/MM/yyyy HH:mm}}</li>
                          <li><strong>Tổng Số Tài Liệu:</strong> {{borrowReq.TotalRequestItem}}</li>
                      </ul>
                      <p><strong>Các Tài Liệu Đã Yêu Cầu:</strong></p>
                      <ul>
                          {{borrowItemList}}
                      </ul>
                  </div>
                  """;
        }

        if (reservationQueues != null && reservationQueues.Any()) // Only process reservation when exist
        {
            // Check whether header message is null or not. If not -> Combined request with reservation
            if (string.IsNullOrEmpty(headerMessage))
                headerMessage = "Yêu Cầu Đặt Trước Sách Thành Công";
            else
                headerMessage += " và Đặt Trước Thành Công";
    
            // Check whether main message is null or not. If not -> Combined request with reservation
            if (string.IsNullOrEmpty(mainMessage))
                mainMessage = "Yêu cầu đặt trước sách của bạn đã được xử lý thành công. Vì một số tài liệu hiện không khả dụng để mượn ngay, bạn đã được thêm vào danh sách đặt trước:"; 
            else
                mainMessage += " Ngoài ra, vì một số tài liệu không khả dụng để mượn ngay, bạn đã được thêm vào danh sách đặt trước:";
            
            // Details of reservation (reserve items after request failed)
            var reservationList = string.Join("", reservationQueues.Select(reservation => 
                $"""
                 <li>
                     <p><strong>Tiêu đề:</strong> <span class="title">{reservation.LibraryItem.Title}</span></p>
                     <p><strong>ISBN:</strong> <span class="isbn">{reservation.LibraryItem.Isbn}</span></p>
                     <p><strong>Ngày Đặt:</strong> {reservation.ReservationDate:dd/MM/yyyy HH:mm}</p>
                     <p>
                         <strong>Ngày Dự Kiến Có Sẵn (Tối thiểu):</strong> 
                         {(reservation.ExpectedAvailableDateMin.HasValue 
                             ? reservation.ExpectedAvailableDateMin.Value.ToString("dd/MM/yyyy HH:mm") 
                             : "Sẽ được cập nhật sau khi có người trả sách hoặc có yêu cầu đặt trước khác")}
                     </p>
                     <p>
                         <strong>Ngày Dự Kiến Có Sẵn (Tối đa):</strong> 
                         {(reservation.ExpectedAvailableDateMax.HasValue 
                             ? reservation.ExpectedAvailableDateMax.Value.ToString("dd/MM/yyyy HH:mm") 
                             : "Sẽ được cập nhật sau khi có người trả sách hoặc có yêu cầu đặt trước khác")}
                     </p>
                     <p><strong>Đặt Sau Yêu Cầu Thất Bại:</strong> {(reservation.IsReservedAfterRequestFailed ? "Có" : "Không")}</p>
                     <p><strong>Tình Trạng Hàng Đợi:</strong> {reservation.QueueStatus.GetDescription()}</p>
                 </li>
                 """));
            
           // Build-up reservation section with unordered list
           reservationSection = $$"""
                  <p><strong>Thông Tin Tài Liệu Đặt Trước:</strong></p>
                  <div class="details">
                      <ul>
                          {{reservationList}}
                      </ul>
                  </div>
                  """;
        }

        return $$"""
             <!DOCTYPE html>
             <html>
             <head>
                 <meta charset="UTF-8">
                 <title>{{headerMessage}}</title>
                 <style>
                     body {
                         font-family: Arial, sans-serif;
                         line-height: 1.6;
                         color: #333;
                     }
                     .header {
                         font-size: 18px;
                         color: #2c3e50;
                         font-weight: bold;
                     }
                     .details {
                         margin: 15px 0;
                         padding: 10px;
                         background-color: #f9f9f9;
                         border-left: 4px solid #27ae60;
                     }
                     .details ul {
                         list-style-type: disc;
                         padding-left: 20px;
                     }
                     .details li {
                         margin: 5px 0;
                     }
                     .footer {
                         margin-top: 20px;
                         font-size: 14px;
                         color: #7f8c8d;
                     }
                     .isbn {
                         color: #2980b9;
                         font-weight: bold;
                     }
                     .title {
                         color: #f39c12;
                         font-weight: bold;
                     }
                 </style>
             </head>
             <body>
                 <p class="header">{{headerMessage}}</p>
                 <p>Xin chào {{user.LastName}} {{user.FirstName}},</p>
                 <p>{{mainMessage}}</p>
                 
                 {{borrowSection}}
                 {{reservationSection}}
                 
                 <p>Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ, vui lòng liên hệ qua số <strong>{{libContact}}</strong>.</p>
                 <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện.</p>
                 
                 <p class="footer"><strong>Trân trọng,</strong></p>
                 <p class="footer">{{libName}}</p>
             </body>
             </html>
             """;
    }
}