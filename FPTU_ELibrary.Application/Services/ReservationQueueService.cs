using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class ReservationQueueService : GenericService<ReservationQueue, ReservationQueueDto, int>,
    IReservationQueueService<ReservationQueueDto>
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecordSvc;
    
    private readonly BorrowSettings _borrowSettings;

    public ReservationQueueService(
        IUserService<UserDto> userSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecordSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        IOptionsMonitor<BorrowSettings> monitor,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cardSvc = cardSvc;
        _userSvc = userSvc;
        _libItemSvc = libItemSvc;
        _borrowRecordSvc = borrowRecordSvc;
        _borrowSettings = monitor.CurrentValue;
    }

    public async Task<IServiceResult> CheckPendingByItemInstanceIdAsync(int itemInstanceId)
    {
        try
        {
            // Build spec
            var libSpec = new BaseSpecification<LibraryItem>(
                li => li.LibraryItemInstances.Any(l => l.LibraryItemInstanceId == itemInstanceId));
            // Retrieve library item by instance id
            var getRes = (await _libItemSvc.GetWithSpecAndSelectorAsync(
                libSpec, selector: lib => lib.LibraryItemId)).Data;
            if (getRes == null || !int.TryParse(getRes.ToString(), out var validItemId))
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<ReservationQueue>(rq =>
                    rq.QueueStatus == ReservationQueueStatus.Pending && // Must be in pending status
                    rq.LibraryItemId == validItemId); // Equals item id
            // Retrieve with spec
            var entities = await _unitOfWork.Repository<ReservationQueue, int>().GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
        }   
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all reservation queue by item instance ids");
        }
    }

    public async Task<IServiceResult> CheckAllowToReserveByItemIdAsync(int itemId)
    {
        try
        {
            // Retrieve library item
            var libSpec = new BaseSpecification<LibraryItem>(li => li.LibraryItemId == itemId);
            // Apply include
            libSpec.ApplyInclude(q => q.Include(li => li.LibraryItemInventory!));
            var libItemDto = (await _libItemSvc.GetWithSpecAsync(libSpec)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                // Not allow to reserve
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<ReservationQueue>(rq =>
                rq.QueueStatus == ReservationQueueStatus.Pending && // Must be in pending status
                rq.LibraryItemId == libItemDto.LibraryItemId); // Equals item id
            // Retrieve with spec
            var entities = (await _unitOfWork.Repository<ReservationQueue, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            if (entities.Any())
            {
                // Check allow to reserve
                if (entities.Count >= libItemDto.LibraryItemInventory?.TotalUnits || // Total reservation exceed or equals to total units
                    libItemDto.LibraryItemInventory?.AvailableUnits > 0) // Still exist available items 
                {
                    // Not allow to reserve
                    return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
                }
            }
            
            // Allow to reserve whether item total units > 0 and available == 0
            if (libItemDto.LibraryItemInventory != null && 
                libItemDto.LibraryItemInventory.TotalUnits > 0 &&
                libItemDto.LibraryItemInventory.TotalUnits > entities.Count && // Total current reservation must smaller than total units of item  
                libItemDto.LibraryItemInventory.AvailableUnits == 0)
            {
                // Allow to reserve
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), true);
            }
            
            // Not allow to reserve
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check allow to reserve by item id");
        }
    }
    
    public async Task<IServiceResult> GetAllCardHolderReservationByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.UserId, userId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestDto>());
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<ReservationQueue>(br => br.LibraryCardId == userDto.LibraryCardId);   
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(rq => rq.LibraryItemInstance)
                .Include(rq => rq.LibraryItem)
            );
            
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.ReservationDate);
            
            // Count total borrow request
            var totalReservationWithSpec = await _unitOfWork.Repository<ReservationQueue, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalReservationWithSpec / pageSize);

            // Set pagination to specification after count total reservation 
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }

            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<ReservationQueue, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Convert to dto collection
                var queueDtos = _mapper.Map<List<ReservationQueueDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderReservationQueueDto>(
                    queueDtos.Select(br => br.ToCardHolderReservationQueueDto()),
                    pageIndex, pageSize, totalPage, totalReservationWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderBorrowRequestDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all reservation queue by user id");
        }
    }

    public async Task<IServiceResult> CreateRangeWithoutSaveChangesAsync(Guid libraryCardId, List<ReservationQueueDto> dtos)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Check exist card
            var cardDto = (await _cardSvc.GetByIdAsync(libraryCardId)).Data as LibraryCardDto;
            if (cardDto == null)
            {
                // Msg: Cannot create item reservation as {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0001);
                return new ServiceResult(ResultCodeConst.Reservation_Warning0001,
                    StringUtils.Format(errMsg, isEng 
                        ? "not found library card" 
                        : "không tìm thấy thẻ thư viện"));
            }
            
            // Check card validity
            var checkCardRes = await _cardSvc.CheckCardValidityAsync(cardDto.LibraryCardId);
            if (checkCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return checkCardRes;
            
            // Look up for the duplicate item
            var libItemIdSet = new HashSet<int>();
            foreach (var libItemId in dtos.Select(r => r.LibraryItemId).ToList())
            {
                if(!libItemIdSet.Add(libItemId))
                {
                    // Msg: Cannot create item reservation as {0}
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0001);
                    return new ServiceResult(ResultCodeConst.Reservation_Warning0001,
                        StringUtils.Format(errMsg, isEng
                            ? "exist duplicate item"
                            : "tồn tại tài liệu bị trùng"));
                }
            }
            
            // Initialize reservation entities
            var entities = new List<ReservationQueue>();
            // Iterate each reservation queue
            foreach (var reservationDto in dtos)
            {
                 // Generate expected available date range for libItemId in requested reservation           
                 var generateResp = await GenerateExpectedAvailableDateAsync(libItemId: reservationDto.LibraryItemId);
                 if (generateResp.ExpectedAvailableDateMin.HasValue &&
                     generateResp.ExpectedAvailableDateMax.HasValue &&
                     generateResp.ExpectedAvailableDateMin.Value < generateResp.ExpectedAvailableDateMax.Value)
                 {
                     // Assign expected date range
                     reservationDto.ExpectedAvailableDateMin = generateResp.ExpectedAvailableDateMin;
                     reservationDto.ExpectedAvailableDateMax = generateResp.ExpectedAvailableDateMax;
                     
                     // Add necessary fields
                     reservationDto.LibraryCardId = cardDto.LibraryCardId;
                     reservationDto.ReservationDate = currentLocalDateTime;
                     reservationDto.QueueStatus = ReservationQueueStatus.Pending;

                     // Add to entity list
                     entities.Add(_mapper.Map<ReservationQueue>(reservationDto));
                 }
            }
            
            // Try to add range without save changes
            await _unitOfWork.Repository<ReservationQueue, int>().AddRangeAsync(entities);
            
            // Mark as create successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when create range reservation queue");
        }
    }

    private async Task<(int LibItemId, DateTime? ExpectedAvailableDateMin, DateTime? ExpectedAvailableDateMax)>
        GenerateExpectedAvailableDateAsync(int libItemId)
    {
        // Retrieve all borrow record, which has record detail containing item instance with specific libItemId 
        var borrowRecords = (await _borrowRecordSvc.GetAllBorrowingByItemIdAsync(itemId: libItemId)
            ).Data as List<BorrowRecordDto>;
        // Extract all active details that belongs to specific libItemId
        var activeDetails = borrowRecords?
            .SelectMany(br => br.BorrowRecordDetails.Where(brd => brd.LibraryItemInstance.LibraryItemId == libItemId))
            .OrderBy(brd => brd.DueDate) // Order by due date
            .ToList();
        // Retrieve all reservation with specific libItemId
        var reserveSpec = new BaseSpecification<ReservationQueue>(rq => rq.LibraryItemId == libItemId);
        var reservations = (await _unitOfWork.Repository<ReservationQueue, int>().GetAllWithSpecAsync(reserveSpec)).ToList();
        
        // Required exist at least borrow record to generate expected date range
        if(activeDetails != null && activeDetails.Any() &&
           // Total borrowing record detail must greater than current total reservation 
           activeDetails.Count > reservations.Count)
        {
            // Sort reservations by reservation date (assuming earlier reservations should be served first)
            reservations = reservations.OrderBy(r => r.ReservationDate).ToList();
            
            // reservation.Count as borrow record detail start index
            for (int i = reservations.Count; i < activeDetails.Count; i++)
            {
                // Extract max extension date
                var maxExtensionDays = _borrowSettings.TotalBorrowExtensionInDays;
                // Extract handling days after returning book
                var handlingDays = _borrowSettings.OverdueOrLostHandleInDays;
                
                // Assuming expected date range for lib item
                var expectedAvailableDateMin = activeDetails[i].DueDate.AddDays(1); // 1 day after returning book
                var expectedAvailableDateMax = activeDetails[i].DueDate.AddDays(maxExtensionDays + handlingDays); // Sum of max extension days and handling days
                
                // Response
                return (libItemId, expectedAvailableDateMin, expectedAvailableDateMax);
            }
        }
        
        // Add default value to response
        return (libItemId, null, null);
    }
}