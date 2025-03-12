using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBorrowRequestService<TDto> : IGenericService<BorrowRequest, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null);
    Task<IServiceResult> CreateAsync(string email, TDto dto, List<int> reservationIds, List<int> userFavoriteIds);
    Task<IServiceResult> CancelAsync(string email, int id, string? cancellationReason);
    Task<IServiceResult> UpdateStatusWithoutSaveChangesAsync(int id, BorrowRequestStatus status);
    Task<IServiceResult> CheckExistBarcodeInRequestAsync(int id, string barcode);
    Task<IServiceResult?> ValidateBorrowAmountAsync(int totalItem, Guid libraryCardId);
}