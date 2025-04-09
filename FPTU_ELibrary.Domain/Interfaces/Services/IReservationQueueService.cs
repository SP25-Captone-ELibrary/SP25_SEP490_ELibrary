using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IReservationQueueService<TDto> : IGenericService<ReservationQueue, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AssignInstancesAfterReturnAsync(List<int> libraryItemInstanceIds);
    Task<IServiceResult> AssignByIdAndInstanceIdAsync(int id, int libraryItemInstanceId);
    Task<IServiceResult> ConfirmApplyLabelAsync(List<int> queueIds);
    Task<IServiceResult> GetAssignableByIdAsync(int id);
    Task<IServiceResult> GetAssignableInstancesAfterReturnAsync(List<int> libraryItemInstanceIds);
    Task<IServiceResult> GetAllAssignableForDashboardAsync(DateTime? startDate, DateTime? endDate, TrendPeriod period, int pageIndex, int pageSize);
    Task<IServiceResult> GetAllCardHolderReservationAsync(ISpecification<ReservationQueue> spec, bool tracked = false);
    Task<IServiceResult> GetAllPendingAndAssignedReservationByLibCardIdAsync(Guid libraryCardId);
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null);
    Task<IServiceResult> GetAppliedLabelByIdAsync(int id, string reservationCode);
    Task<IServiceResult> ReapplyLabelByIdAsync(int id, string reservationCode);
    Task<IServiceResult> ExtendPickupDateAsync(int id);
    Task<IServiceResult> CountAllReservationByLibCardIdAndStatusAsync(Guid libraryCardId, ReservationQueueStatus status);
    Task<IServiceResult> CheckPendingByItemInstanceIdAsync(int itemInstanceId);
    Task<IServiceResult> CheckAllowToReserveByItemIdAsync(int itemId, string email);
    Task<IServiceResult> CreateRangeWithoutSaveChangesAsync(Guid libraryCardId, List<TDto> dtos);
    Task<IServiceResult> UpdateReservationToCollectedWithoutSaveChangesAsync(int id, int libraryItemInstanceId);
}
