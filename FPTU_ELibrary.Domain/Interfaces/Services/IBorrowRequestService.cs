using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBorrowRequestService<TDto> : IGenericService<BorrowRequest, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null);
    Task<IServiceResult> GetAllPendingRequestByLibCardIdAsync(Guid libraryCardId);
    Task<IServiceResult> CountAllPendingRequestByLibCardIdAsync(Guid libraryCardId);
    Task<IServiceResult> CreateAsync(string email, TDto dto, 
        List<int> reservationItemIds,
        List<int> resourceIds);
    Task<IServiceResult> AddItemAsync(string email, int id, int libraryItemId);
    Task<IServiceResult> CancelAsync(string email, int id, string? cancellationReason, bool isConfirmed = false);
    Task<IServiceResult> CancelManagementAsync(Guid libraryCardId, int id, string? cancellationReason, bool isConfirmed = false);
    Task<IServiceResult> CancelSpecificItemAsync(string email, int id, int libraryItemId);
    Task<IServiceResult> CancelSpecificItemManagementAsync(Guid libraryCardId, int id, int libraryItemId);
    Task<IServiceResult> UpdateStatusWithoutSaveChangesAsync(int id, BorrowRequestStatus status);
    Task<IServiceResult> CheckExistBarcodeInRequestAsync(int id, string barcode);
    Task<IServiceResult?> ValidateBorrowAmountAsync(int totalItem, Guid libraryCardId,
        bool isCallFromRecordSvc = false, int totalActualItem = 0);
}