using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IReservationQueueService<TDto> : IGenericService<ReservationQueue, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateRangeWithoutSaveChangesAsync(Guid libraryCardId, List<TDto> dtos);
    Task<IServiceResult> GetAllCardHolderReservationByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<IServiceResult> CheckPendingByItemInstanceIdAsync(int itemInstanceId);
    Task<IServiceResult> CheckAllowToReserveByItemIdAsync(int itemId, string email);
}