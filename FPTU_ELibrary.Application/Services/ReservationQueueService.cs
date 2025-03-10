using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class ReservationQueueService : GenericService<ReservationQueue, ReservationQueueDto, int>,
    IReservationQueueService<ReservationQueueDto>
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;

    public ReservationQueueService(
        IUserService<UserDto> userSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _libItemSvc = libItemSvc;
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
    
}