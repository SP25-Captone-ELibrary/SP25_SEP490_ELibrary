using System.Diagnostics.Tracing;
using System.Globalization;
using CloudinaryDotNet.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Users;
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
using HtmlAgilityPack;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class ReservationQueueService : GenericService<ReservationQueue, ReservationQueueDto, int>,
    IReservationQueueService<ReservationQueueDto>
{
    // Lazy services
    private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecordSvc;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowRequestSvc;
    private readonly Lazy<ILibraryItemInventoryService<LibraryItemInventoryDto>> _inventorySvc;
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    
    private readonly IEmailService _emailSvc;
    private readonly IUserService<UserDto> _userSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;
    
    private readonly AppSettings _appSettings;
    private readonly BorrowSettings _borrowSettings;

    public ReservationQueueService(
        // Lazy services
        Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecordSvc,
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowRequestSvc,
        Lazy<ILibraryItemInventoryService<LibraryItemInventoryDto>> inventorySvc,
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        
        IUserService<UserDto> userSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
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
        _emailSvc = emailSvc;
        _libItemSvc = libItemSvc;
        _inventorySvc = inventorySvc;
        _itemInstanceSvc = itemInstanceSvc;
        _borrowRecordSvc = borrowRecordSvc;
        _borrowRequestSvc = borrowRequestSvc;
        
        _appSettings = monitor1.CurrentValue;
        _borrowSettings = monitor.CurrentValue;
    }

    public async Task<IServiceResult> AssignReturnItemAsync(List<int> libraryItemInstanceIds)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            	// Vietnam timezone
            	TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Clone requested item instances to store unhandled instance 
            var unhandledInstanceIds = libraryItemInstanceIds.Clone();
            // Initialize handled reservation dic to process send email
            var handledReservationDic = new Dictionary<Guid, List<ReservationQueueDto>>();
            // Try to retrieve pending reservation order ascending by reserve date
            var baseSpec = new BaseSpecification<ReservationQueue>(r =>
                // Pending status
                r.QueueStatus == ReservationQueueStatus.Pending &&
                // Not assigned any item instance
                r.LibraryItemInstanceId == null &&
                // With all item's instances contain return instance id
                r.LibraryItem.LibraryItemInstances.Any(li => libraryItemInstanceIds.Contains(li.LibraryItemInstanceId)));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(r => r.LibraryItem)
                    .ThenInclude(l => l.LibraryItemInventory)
                .Include(r => r.LibraryItem)
                    .ThenInclude(l => l.LibraryItemInstances)
                .Include(q => q.LibraryCard)
            );
            // Order by reservation date
            baseSpec.AddOrderBy(r => r.ReservationDate);
            // Retrieve all with spec 
            var entities = (await _unitOfWork.Repository<ReservationQueue, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            if (entities.Any())
            {
                // Iterate each reservation to check for constraint
                foreach (var reservation in entities)
                {
                    // Stop foreach when unhandled instance equals to 0
                    if (unhandledInstanceIds.Count == 0) break;
                    
                    // Check current user library activity
                    if ((await _userSvc.GetPendingLibraryActivitySummaryAsync(
                            libraryCardId: reservation.LibraryCardId)).Data is UserPendingActivitySummaryDto userActivitySummary)
                    {
                        // Check whether user's active/pending activity borrow/reserve reaching threshold
                        var isReachedThreshold = userActivitySummary.RemainTotal == 0;
                        // Skip to another reservation
                        if(isReachedThreshold) continue;
                        else // Assign item instance to the reservation
                        {
                            // Extract all library item instance ids of reservation's item
                            var existingInstanceIds = reservation.LibraryItem.LibraryItemInstances
                                .Select(li => li.LibraryItemInstanceId)
                                .ToList();
                            
                            // Iterate each return instance ids to check whether existing in reservation's item instance ids
                            for (int i = 0; i < unhandledInstanceIds.Count; ++i)
                            {
                                // Returned instance belongs to current reservation
                                if (existingInstanceIds.Contains(unhandledInstanceIds[i])) 
                                {
                                    // Process update reservation status
                                    reservation.QueueStatus = ReservationQueueStatus.Assigned;
                                    reservation.LibraryItemInstanceId = unhandledInstanceIds[i];
                                    // Create expected available from-to date for user (add 1 more day allowing library to handle unexpected issue)
                                    // TODO: Reimplement generating expected available date with more reliable (check for occasions, sunday, etc.)
                                    var expectedAvailableDate = currentLocalDateTime.AddDays(1);
                                    reservation.ExpectedAvailableDateMin = expectedAvailableDate; 
                                    reservation.ExpectedAvailableDateMax = expectedAvailableDate;
                                    // Add pick-up expiry date 
                                    reservation.ExpiryDate = expectedAvailableDate.AddDays(_borrowSettings.PickUpExpirationInDays);
                                    // Set default is notified is false to allow background svc handle remind user about pick up expiry date (before 24 hrs)
                                    reservation.IsNotified = false;
                                    // Save updated entity
                                    await _unitOfWork.Repository<ReservationQueue, int>().UpdateAsync(reservation);
                                    
                                    // Process update item instance status
                                    await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
                                        libraryItemInstanceIds: [unhandledInstanceIds[i]],
                                        status: LibraryItemInstanceStatus.Reserved,
                                        isProcessBorrowRequest: false);
                                    
                                    // Break and remove handled index
                                    unhandledInstanceIds.RemoveAt(i); 
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // Group handled reservation by library card to check for exceed than threshold
                var groupedReservations = entities.GroupBy(g => g.LibraryCardId).ToList();
                foreach (var groupedRes in groupedReservations)
                {
                    // Extract all reservation existing reservation code
                    var reservationsFromGroup = groupedRes.ToList();
                    // Recheck for card's threshold value
                    var activitySummary = (await _userSvc.GetPendingLibraryActivitySummaryAsync(libraryCardId: groupedRes.Key)).Data;
                    if (activitySummary is UserPendingActivitySummaryDto userActivitySummary)
                    {
                        // Compared card's handled reservation with remain total
                        if (reservationsFromGroup.Count > userActivitySummary.RemainTotal)
                        {
                            // Subtract to get exceed value
                            var exceedValue = reservationsFromGroup.Count - userActivitySummary.RemainTotal;
                            // Try to remove handled reservation based on exceed value
                            for (int i = 0; i < exceedValue; ++i)
                            {
                                var reservation = reservationsFromGroup[i];

                                if (int.TryParse(reservation.LibraryItemInstanceId.ToString(), out var validInstanceId))
                                {
                                    // Update instance status to default (out-of-shelf)
                                    await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
                                        libraryItemInstanceIds: [validInstanceId],
                                        status: LibraryItemInstanceStatus.OutOfShelf,
                                        isProcessBorrowRequest: false);
                                }
                                    
                                // Set default reservation
                                reservation.QueueStatus = ReservationQueueStatus.Pending;
                                reservation.LibraryItemInstanceId = null;
                                reservation.ExpectedAvailableDateMin = null; 
                                reservation.ExpectedAvailableDateMax = null;
                                reservation.ExpiryDate = null;
                                reservation.IsNotified = false;
                                
                                // Add to unhandled list
                                unhandledInstanceIds.Add(validInstanceId);
                                
                                // Process update reservation
                                await _unitOfWork.Repository<ReservationQueue, int>().UpdateAsync(reservation);
                            }
                        }
                        
                        // Retrieve all existing assigned reservation
                        var reserveSpec = new BaseSpecification<ReservationQueue>(r =>
                            // Has already assigned instance
                            (r.QueueStatus == ReservationQueueStatus.Assigned || 
                             r.QueueStatus == ReservationQueueStatus.Expired) &&  
                            r.LibraryItemInstanceId != null &&
                            // Has already registered reservation code
                            r.ReservationCode != null                        
                        );
                        // Retrieve today's reservations with spec
                        var todayAssignedReservations = (await _unitOfWork.Repository<ReservationQueue, int>()
                            .GetAllWithSpecAsync(reserveSpec)).ToList();
                        // Generate reservation code
                        var newReservationCode = GenerateNextReservationCode(todayAssignedReservations);
                        if (!string.IsNullOrEmpty(newReservationCode))
                        {
                            var assignedReservations = reservationsFromGroup
                                .Where(r => r.QueueStatus == ReservationQueueStatus.Assigned)
                                .ToList();
                            // Iterate all assigned status in grouped reservation
                            assignedReservations.ForEach(r =>
                                {
                                    // Add default assign handling fields
                                    r.ReservationCode = newReservationCode;
                                    r.IsAppliedLabel = false;
                                });
                            
                            // Add to handled reservation dic
                            handledReservationDic.Add(
                                key: groupedRes.Key,
                                value: _mapper.Map<List<ReservationQueueDto>>(assignedReservations));
                        }
                    }
                }

                // Process save changes when exist at least one instance has been handled 
                if (groupedReservations.Any() && 
                    (unhandledInstanceIds.Count == 0 ||
                     unhandledInstanceIds.Count < libraryItemInstanceIds.Count))
                {
                    // Save DB
                    var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
                    if (isSaved)
                    {
                        // Process send email
                        foreach (var key in handledReservationDic.Keys)
                        {
                            if (handledReservationDic.TryGetValue(key, out var reservations))
                            {
                                if(reservations.Any())
                                {
                                    // Retrieve reservation code
                                    var reservationCode = reservations[0].ReservationCode;
                                    
                                    // Retrieve user information
                                    var userSpec = new BaseSpecification<User>(u => u.LibraryCardId == key);
                                    // Apply include
                                    userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));

                                    // Process send email to announce about assigned items
                                    if ((await _userSvc.GetWithSpecAsync(userSpec)).Data is UserDto userDto &&
                                        userDto.LibraryCard != null)
                                    {
                                        await SendAssignReservationSuccessEmailBody(
                                            email: userDto.Email,
                                            libCard: userDto.LibraryCard,
                                            assignedReservations: reservations,
                                            reservationCode: reservationCode ?? string.Empty,
                                            libName: _appSettings.LibraryName,
                                            libContact: _appSettings.LibraryContact);
                                    }
                                    else
                                    {
                                        _logger.Error("Failed to send assigned reservation email to user");
                                    }
                                }
                            }
                        }
                        
                        // Map to dto
                        var dtoList = _mapper.Map<List<ReservationQueueDto>>(handledReservationDic.Values
                            .SelectMany(list => list).ToList()); // Select all values in dictionary
                        // Convert to collection of assign result
                        var assignReservationResultList = dtoList.ToAssignReservationResultDto(assignedDate: currentLocalDateTime);
                        
                        // Response with assign result
                        var successMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0008);
                        return new ServiceResult(ResultCodeConst.Borrow_Success0008,
                            message: StringUtils.Format(successMsg, libraryItemInstanceIds.Count.ToString()),
                            data: assignReservationResultList);
                    }   
                    else
                    {
                        _logger.Error("Failed to save assign item instance to reservation queues");
                    }
                }
            }

            // Do nothing, keep instance in outOfShelf status waiting for librarian update in shelf
            // Response return items success
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0008);
            return new ServiceResult(ResultCodeConst.Borrow_Success0008,
                StringUtils.Format(msg, libraryItemInstanceIds.Count.ToString()));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process assign return item to specific reservation");
        }
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

    public async Task<IServiceResult> CheckAllowToReserveByItemIdAsync(int itemId, string email)
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
            
            // Check allow to reserve
            if (libItemDto.LibraryItemInventory?.AvailableUnits > 0) // Still exist available items 
            {
                // Msg: Cannot reserve for item {0} as this item is still available to borrow
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0005); 
                return new ServiceResult(ResultCodeConst.Reservation_Warning0005,
                    StringUtils.Format(errMsg, $"'{libItemDto.Title}'"), false);
            }
            
            // Retrieve user 
            var userDto = (await _userSvc.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null)
            {
                // Msg: Not allow to reserve
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
            }else if (userDto.LibraryCardId == null)
            {
                // Msg: You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004), false);
            }
            
            // Check whether user has already reserved item
            var isAlreadyReserved = await _unitOfWork.Repository<ReservationQueue, int>().AnyAsync(r =>
                r.LibraryItemId == itemId &&
                r.LibraryCardId == userDto.LibraryCardId &&
                r.QueueStatus != ReservationQueueStatus.Expired &&
                r.QueueStatus != ReservationQueueStatus.Cancelled &&
                r.QueueStatus != ReservationQueueStatus.Collected);
            if (isAlreadyReserved)
            {
                // Msg: You have already reserved item {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0004);
                return new ServiceResult(ResultCodeConst.Reservation_Warning0004,
                    StringUtils.Format(errMsg, $"'{libItemDto.Title}'"), false);
            }
            
            // Check whether user has already borrowed item
            var isAlreadyBorrowed = (await _borrowRecordSvc.Value.AnyAsync(bRec =>
                bRec.LibraryCardId == userDto.LibraryCardId &&
                bRec.BorrowRecordDetails.Any(brd =>
                    brd.LibraryItemInstance.LibraryItemId == itemId && // Exist in any borrow record details
                    brd.Status != BorrowRecordStatus.Returned))).Data is true; // Exclude elements with returned status
            if (isAlreadyBorrowed)
            {
                // Msg: Cannot reserve for item {0} as you are borrowing this item
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0003);
                return new ServiceResult(ResultCodeConst.Reservation_Warning0003,
                    StringUtils.Format(errMsg, $"'{libItemDto.Title}'"), false);
            }
            
            // Check whether user has already requested item
            var isAlreadyRequested = (await _borrowRequestSvc.Value.AnyAsync(
                br => br.LibraryCardId == userDto.LibraryCardId &&
                      br.Status == BorrowRequestStatus.Created &&
                      br.BorrowRequestDetails.Any(brd => brd.LibraryItemId == itemId))).Data is true;
            if (isAlreadyRequested)
            {
                // Msg: Cannot reserve for item {0} as you are borrowing this item
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0003);
                return new ServiceResult(ResultCodeConst.Reservation_Warning0003,
                    StringUtils.Format(errMsg, $"'{libItemDto.Title}'"), false);
            }

            #region Archived code
            // // Build spec
            // var baseSpec = new BaseSpecification<ReservationQueue>(rq =>
            //     rq.QueueStatus == ReservationQueueStatus.Pending && // Must be in pending status
            //     rq.LibraryItemId == libItemDto.LibraryItemId); // Equals item id
            // // Retrieve with spec
            // var entities = (await _unitOfWork.Repository<ReservationQueue, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            // if (entities.Any())
            // {
            //     // Check allow to reserve
            //     if (libItemDto.LibraryItemInventory?.AvailableUnits > 0) // Still exist available items 
            //     {
            //         // Msg: Cannot reserve for item {0} as this item is still available to borrow
            //         var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Warning0005); 
            //         return new ServiceResult(ResultCodeConst.Reservation_Warning0005,
            //             StringUtils.Format(errMsg, $"'{libItemDto.Title}'"), false);
            //     }
            // }
            //
            // // Allow to reserve whether item total units > 0 and available == 0
            // if (libItemDto.LibraryItemInventory != null && 
            //     libItemDto.LibraryItemInventory.TotalUnits > 0 &&
            //     libItemDto.LibraryItemInventory.TotalUnits > entities.Count && // Total current reservation must smaller than total units of item  
            //     libItemDto.LibraryItemInventory.AvailableUnits == 0 &&
            //     // Only allow to reserve when at least one item is borrowing
            //     libItemDto.LibraryItemInventory.BorrowedUnits > 0 && 
            //     libItemDto.LibraryItemInventory.BorrowedUnits > libItemDto.LibraryItemInventory.ReservedUnits)
            // {
            //     // Allow to reserve
            //     return new ServiceResult(ResultCodeConst.SYS_Warning0004,
            //         await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), true);
            // }
            #endregion
            
            // Mark as allow to borrow
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check allow to reserve by item id");
        }
    }

    public async Task<IServiceResult> CountAllReservationByLibCardIdAndStatusAsync(Guid libraryCardId, ReservationQueueStatus status)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<ReservationQueue>(r =>
                r.LibraryCardId == libraryCardId && // With specific lib card
                r.QueueStatus == status &&
                // Exclude cancellation fields
                r.CancellationReason == null && 
                r.CancelledBy == null
            );
            // Count all pending request
            var countRes = await _unitOfWork.Repository<ReservationQueue, int>().CountAsync(baseSpec);
            // Response
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), countRes);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process count all pending and assigned reservation by lib card id");
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
                new List<LibraryCardHolderReservationQueueDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all reservation queue by user id");
        }
    }

    public async Task<IServiceResult> GetAllPendingAndAssignedReservationByLibCardIdAsync(Guid libraryCardId)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<ReservationQueue>(r =>
            	r.LibraryCardId == libraryCardId && // With specific lib card
            	(
                    r.QueueStatus == ReservationQueueStatus.Pending || // In pending status
                    r.QueueStatus == ReservationQueueStatus.Assigned  // In assigned status
                ) && 
                // Exclude cancellation fields
                r.CancellationReason == null && 
                r.CancelledBy == null
            );
            // Retrieve all with spec and selector
            var entities = (await _unitOfWork.Repository<ReservationQueue, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, selector: r => new ReservationQueue()
                {
                    QueueId = r.QueueId,
                    LibraryItemId = r.LibraryItemId,
                    LibraryItemInstanceId = r.LibraryItemInstanceId,
                    LibraryCardId = r.LibraryCardId,
                    QueueStatus = r.QueueStatus,
                    BorrowRequestId = r.BorrowRequestId,
                    IsReservedAfterRequestFailed = r.IsReservedAfterRequestFailed,
                    ExpectedAvailableDateMin = r.ExpectedAvailableDateMin,
                    ExpectedAvailableDateMax = r.ExpectedAvailableDateMax,
                    ReservationDate = r.ReservationDate,
                    ExpiryDate = r.ExpiryDate,
                    CollectedDate = r.CollectedDate,
                    IsNotified = r.IsNotified,
                    CancelledBy = r.CancelledBy,
                    CancellationReason = r.CancellationReason,
                    LibraryItem = new LibraryItem()
	                {
	                    LibraryItemId = r.LibraryItem.LibraryItemId,
	                    Title = r.LibraryItem.Title,
	                    SubTitle = r.LibraryItem.SubTitle,
	                    Responsibility = r.LibraryItem.Responsibility,
	                    Edition = r.LibraryItem.Edition,
	                    EditionNumber = r.LibraryItem.EditionNumber,
	                    Language = r.LibraryItem.Language,
	                    OriginLanguage = r.LibraryItem.OriginLanguage,
	                    Summary = r.LibraryItem.Summary,
	                    CoverImage = r.LibraryItem.CoverImage,
	                    PublicationYear = r.LibraryItem.PublicationYear,
	                    Publisher = r.LibraryItem.Publisher,
	                    PublicationPlace = r.LibraryItem.PublicationPlace,
	                    ClassificationNumber = r.LibraryItem.ClassificationNumber,
	                    CutterNumber = r.LibraryItem.CutterNumber,
	                    Isbn = r.LibraryItem.Isbn,
	                    Ean = r.LibraryItem.Ean,
	                    EstimatedPrice = r.LibraryItem.EstimatedPrice,
	                    PageCount = r.LibraryItem.PageCount,
	                    PhysicalDetails = r.LibraryItem.PhysicalDetails,
	                    Dimensions = r.LibraryItem.Dimensions,
	                    AccompanyingMaterial = r.LibraryItem.AccompanyingMaterial,
	                    Genres = r.LibraryItem.Genres,
	                    GeneralNote = r.LibraryItem.GeneralNote,
	                    BibliographicalNote = r.LibraryItem.BibliographicalNote,
	                    TopicalTerms = r.LibraryItem.TopicalTerms,
	                    AdditionalAuthors = r.LibraryItem.AdditionalAuthors,
	                    CategoryId = r.LibraryItem.CategoryId,
	                    ShelfId = r.LibraryItem.ShelfId,
	                    GroupId = r.LibraryItem.GroupId,
	                    Status = r.LibraryItem.Status,
	                    IsDeleted = r.LibraryItem.IsDeleted,
	                    IsTrained = r.LibraryItem.IsTrained,
	                    CanBorrow = r.LibraryItem.CanBorrow,
	                    TrainedAt = r.LibraryItem.TrainedAt,
	                    CreatedAt = r.LibraryItem.CreatedAt,
	                    UpdatedAt = r.LibraryItem.UpdatedAt,
	                    UpdatedBy = r.LibraryItem.UpdatedBy,
	                    CreatedBy = r.LibraryItem.CreatedBy,
	                    // References
	                    Category = r.LibraryItem.Category,
	                    Shelf = r.LibraryItem.Shelf,
	                    LibraryItemInstances = r.LibraryItem.LibraryItemInstances
		                    .Where(lii => lii.LibraryItemInstanceId == r.LibraryItemInstanceId).ToList(),
	                    LibraryItemInventory = r.LibraryItem.LibraryItemInventory,
	                    LibraryItemReviews = r.LibraryItem.LibraryItemReviews,
	                    LibraryItemAuthors = r.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
	                    {
	                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
	                        LibraryItemId = ba.LibraryItemId,
	                        AuthorId = ba.AuthorId,
	                        Author = ba.Author
	                    }).ToList()
	                },
                    LibraryItemInstance = r.LibraryItemInstance != null 
                        ? new LibraryItemInstance()
                        {
                            LibraryItemInstanceId = r.LibraryItemInstance.LibraryItemInstanceId,
                            LibraryItemId = r.LibraryItemInstance.LibraryItemId,
                            Barcode = r.LibraryItemInstance.Barcode,
                            Status = r.LibraryItemInstance.Status,
                            CreatedAt = r.LibraryItemInstance.CreatedAt,
                            UpdatedAt = r.LibraryItemInstance.UpdatedAt,
                            CreatedBy = r.LibraryItemInstance.CreatedBy,
                            UpdatedBy = r.LibraryItemInstance.UpdatedBy,
                            IsDeleted = r.LibraryItemInstance.IsDeleted,
                            IsCirculated = r.LibraryItemInstance.IsCirculated
                        }
                        : null,
                })).ToList();
            if (entities.Any())
            {
                // Map to dto
                var dtoList = _mapper.Map<List<ReservationQueueDto>>(entities);
                // Convert to GetBorrowRequestDto
                var reservationDtoList = dtoList.Select(e => e.ToGetReservationQueueDto()).ToList();
                // Msg: Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), reservationDtoList);
            }
            
            // Msg: Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<GetBorrowRequestDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all pending and assigned reservation by lib card id");
        }
    }
    
    public async Task<IServiceResult> UpdateReservationToCollectedWithoutSaveChangesAsync(int id, int libraryItemInstanceId)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Retrieve queue by id
            var reserveSpec = new BaseSpecification<ReservationQueue>(r => r.QueueId == id);
            // Apply include
            reserveSpec.ApplyInclude(q => q.Include(r => r.LibraryItemInstance!));
            var existingEntity = await _unitOfWork.Repository<ReservationQueue, int>()
                .GetWithSpecAsync(reserveSpec);
            if (existingEntity == null)
            {
                _logger.Warning("Failed to update reservation to collected status as not found reservation match id {0}", id);
                
                // Msg: The instance has been reserved by the reader, but failed to update the reservation status while create borrow record
                return new ServiceResult(ResultCodeConst.Reservation_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.Reservation_Fail0001), false);
            }
            
            // Check exist library item instance id
            if (existingEntity.LibraryItemInstanceId != null &&
                existingEntity.LibraryItemInstanceId != libraryItemInstanceId)
            {
                // Try to retrieve requested item instance 
                var requestedInstance = (await _itemInstanceSvc.Value.GetByIdAsync(libraryItemInstanceId)).Data as LibraryItemInstanceDto;
                if (requestedInstance == null)
                {
                    _logger.Warning("Failed to update reservation to collected status as not found item instance match id {0}", libraryItemInstanceId);
                    // Mark as update failed
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
                }
                
                // Msg: Reader has already reserved this item and have been assigned with instance {0}. Requested instance {1} is not match
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0036);
                return new ServiceResult(ResultCodeConst.Borrow_Warning0036,
                    StringUtils.Format(
                        errMsg,
                        $"'{existingEntity.LibraryItemInstance!.Barcode}'",
                        $"'{requestedInstance.Barcode}'"), false);
            }
            
            // Change status
            existingEntity.QueueStatus = ReservationQueueStatus.Collected;
            // Assign collected date
            existingEntity.CollectedDate = currentLocalDateTime;
            // Process update without save
            await _unitOfWork.Repository<ReservationQueue, int>().UpdateAsync(existingEntity);
            
            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update reservation to collected status");
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
                 }
                 
                 // Add necessary fields
                 reservationDto.LibraryCardId = cardDto.LibraryCardId;
                 reservationDto.ReservationDate = currentLocalDateTime;
                 reservationDto.QueueStatus = ReservationQueueStatus.Pending;

                 // Add to entity list
                 entities.Add(_mapper.Map<ReservationQueue>(reservationDto));
                 
                 // Retrieving item inventory 
                 var itemIven =
                     (await _inventorySvc.Value.GetByIdAsync(id: reservationDto.LibraryItemId)).Data as LibraryItemInventoryDto;
                 if (itemIven == null)
                 {
                     // Unknown error
                     return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                         await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                 }
                 
                 // Increase reservation value
                 itemIven.ReservedUnits++;
                 // Process update without save change
                 await _inventorySvc.Value.UpdateWithoutSaveChangesAsync(itemIven);
            }
            
            // Try to add range without save changes
            // await _unitOfWork.Repository<ReservationQueue, int>().AddRangeAsync(entities);
            
            // Mark as create successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), data: dtos);
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
        var borrowRecords = (await _borrowRecordSvc.Value.GetAllBorrowingByItemIdAsync(itemId: libItemId)
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

    private string GenerateNextReservationCode(List<ReservationQueue> reservations)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Format date to yyyyMMdd
            var todayStr = currentLocalDateTime.ToString("yyyyMMdd");
            // Initialize max number
            var maxNum = 0;
            
            // Iterate each reservation to retrieve for newest reservation code
            foreach (var reservation in reservations)
            {
                // Skip reservation has null or empty code
                if(string.IsNullOrEmpty(reservation.ReservationCode))
                    continue;
                
                // Split code, and assume that code has 3 parts
                // Code format: RS-yyyyMMdd-XXXX with X: number
                var parts = reservation.ReservationCode.Split('-');
                if (parts.Length == 3 && parts[1] == todayStr) 
                {
                    if (int.TryParse(parts[2], out int number))
                    {
                        if(number > maxNum)
                            maxNum = number;
                    }
                }
            }
            
            // Process generate new code
            var newMaxNum = maxNum + 1;
            var newReservationCode = $"RS-{todayStr}-{newMaxNum:D4}";
            return newReservationCode;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process generate next reservation code");
        }
    }

    private async Task<bool> SendAssignReservationSuccessEmailBody(
        string email,
        LibraryCardDto libCard,
        List<ReservationQueueDto> assignedReservations,
        string reservationCode,
        string libName, string libContact)
    	{
    		try
    		{
    			// Email subject
    			var subject = "[ELIBRARY] Thông Báo Tài Liệu Đặt Trước Đã Có Sẵn";
    			
    			// Process send email
    			var emailMessageDto = new EmailMessageDto( // Define email message
                    // Define Recipient
                    to: new List<string>() { email },
                    // Define subject
                    subject: subject,
                    // Add email body content
                    content: GetAssignReservationSuccessEmailBody(
                        libCard: libCard,
                        assignedReservations: assignedReservations,
                        reservationCode: reservationCode,
                        libName: libName,
                        libContact:libContact)
                );
    			
    			// Process send email
    			return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
    		}
    		catch (Exception ex)
    		{
    			_logger.Error(ex.Message);
    			throw new Exception("Error invoke when process send borrow record return success email");
    		}
    	}
    
    private string GetAssignReservationSuccessEmailBody(
        LibraryCardDto libCard,
        List<ReservationQueueDto> assignedReservations,
        string reservationCode,
        string libName, string libContact)
    {
        // Initialize email content sections
        string headerMessage = string.Empty;
        string mainMessage = string.Empty;
        string codeSection = string.Empty;
        string reservationSection = string.Empty;

        // Process assigned reservations
        if (assignedReservations.Any())
        {
            headerMessage = "Thông Báo Tài Liệu Đặt Trước Đã Có Sẵn";
            mainMessage = "Các tài liệu bạn đã đặt trước đã có sẵn. Vui lòng đến nhận theo thời gian quy định dưới đây:";

            // Build the reservation code section
            codeSection = $"""
                <div style="text-align: center; margin: 20px 0;">
                    <div style="display: inline-block; padding: 10px 20px; background-color: #ffeb3b; border-radius: 8px;">
                        <p style="font-size: 16px; color: #d32f2f; font-weight: bold;">Mã Đặt: {reservationCode}</p>
                    </div>
                    <p style="font-size: 14px; color: #616161;">Vui lòng cung cấp mã đặt này cho thủ thư để nhận các tài liệu.</p>
                </div>
            """;

            // Build a list of reservation details
            var reservationItemList = string.Join("", assignedReservations.Select(reservation =>
            {
                // If an item instance is assigned, include its details (e.g. Barcode and Status)
                string instanceDetails = string.Empty;
                if (reservation.LibraryItemInstance != null)
                {
                    // Try parse status
                    Enum.TryParse(reservation.LibraryItemInstance.Status, true,
                        out LibraryItemInstanceStatus validStatus);
                    
                    instanceDetails = $"""
                        <p><strong>Mã Vạch:</strong> <span class="barcode">{reservation.LibraryItemInstance.Barcode}</span></p>
                        <p><strong>Trạng Thái:</strong> <span class="status-text">{(validStatus != null! ? validStatus.GetDescription() : reservation.LibraryItemInstance.Status)}</span></p>
                    """;
                }

                // Build details for each reserved item
                return $"""
                <li>
                    <p><strong>Tiêu đề:</strong> <span class="title">{reservation.LibraryItem.Title}</span></p>
                    <p><strong>ISBN:</strong> <span class="isbn">{reservation.LibraryItem.Isbn}</span></p>
                    <p><strong>Năm Xuất Bản:</strong> {reservation.LibraryItem.PublicationYear}</p>
                    <p><strong>Nhà Xuất Bản:</strong> {reservation.LibraryItem.Publisher}</p>
                    {instanceDetails}
                    <p><strong>Hạn Nhận:</strong> <span class="expiry-date">{(reservation.ExpiryDate.HasValue 
                            ? reservation.ExpiryDate.Value.ToString("dd/MM/yyyy HH:mm") 
                            : "Không xác định")}</span></p>
                </li>
                """;
            }));

            reservationSection = $"""
                <p><strong>Thông Tin Tài Liệu Đã Cấp:</strong></p>
                <div class="details">
                    <ul>
                        {reservationItemList}
                    </ul>
                </div>
                """;
        }

        // Build HTML content
        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>{headerMessage}</title>
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
                .expiry-date {
                    color: #27ae60;
                    font-weight: bold;
                }
                .status-text {
                    color: #c0392b;
                    font-weight: bold;
                }
                .barcode {
                    color: #8e44ad;
                    font-weight: bold;
                }
                .code {
                    color: #16a085;
                    font-weight: bold;
                }
            </style>
        </head>
        <body>
            <p class="header">{{headerMessage}}</p>
            <p>Xin chào {{libCard.FullName}},</p>
            <p>{{mainMessage}}</p>
            
            {{codeSection}}
            
            {{reservationSection}}
            
            <p>Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ, vui lòng liên hệ qua email: <strong>{{libContact}}</strong>.</p>
            <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện.</p>
            
            <p class="footer"><strong>Trân trọng,</strong></p>
            <p class="footer">{{libName}}</p>
        </body>
        </html>
        """;
    }
}