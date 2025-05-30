using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBorrowRecordService<TDto> : IGenericService<BorrowRecord, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null);
    Task<IServiceResult> GetAllBorrowSettingValuesAsync();
    Task<IServiceResult> GetAllBorrowingByItemIdAsync(int itemId);
    Task<IServiceResult> GetAllActiveRecordByLibCardIdAsync(Guid libraryCardId);
    Task<IServiceResult> GetAllPendingAndExpiredFineAsync(int id);
    Task<IServiceResult> CreateAsync(string processedByEmail, TDto dto);
    Task<IServiceResult> SelfCheckoutAsync(Guid libraryCardId, TDto dto);
    Task<IServiceResult> ProcessRequestToBorrowRecordAsync(string processedByEmail, TDto dto);
    Task<IServiceResult> ProcessReturnAsync(string processedReturnByEmail, Guid libraryCardId,
        TDto recordWithReturnItems, TDto recordWithLostItems, bool isConfirmMissing);
    Task<IServiceResult> ExtendAsync(string email, int borrowRecordId, List<int> borrowRecordDetailIds);
    Task<IServiceResult> ExtendAsync(int borrowRecordId, int borrowRecordDetailId);
    Task<IServiceResult> CalculateBorrowReturnSummaryAsync(string email);
    Task<IServiceResult> CountAllActiveRecordByLibCardIdAsync(Guid libraryCardId);
}