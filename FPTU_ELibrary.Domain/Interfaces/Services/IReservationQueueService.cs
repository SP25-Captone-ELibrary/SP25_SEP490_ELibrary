using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IReservationQueueService<TDto> : IGenericService<ReservationQueue, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AssignReturnItemAsync(List<int> libraryItemInstanceIds);
    Task<IServiceResult> CreateRangeWithoutSaveChangesAsync(Guid libraryCardId, List<TDto> dtos);
    Task<IServiceResult> GetAllCardHolderReservationByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<IServiceResult> GetAllPendingAndAssignedReservationByLibCardIdAsync(Guid libraryCardId);
    Task<IServiceResult> CountAllReservationByLibCardIdAndStatusAsync(Guid libraryCardId, ReservationQueueStatus status);
    Task<IServiceResult> CheckPendingByItemInstanceIdAsync(int itemInstanceId);
    Task<IServiceResult> CheckAllowToReserveByItemIdAsync(int itemId, string email);
    Task<IServiceResult> UpdateReservationToCollectedWithoutSaveChangesAsync(int id, int libraryItemInstanceId);
    // TODO: Add function allowing to retrieve all reservations by library item id (Item Detail Page)
}
