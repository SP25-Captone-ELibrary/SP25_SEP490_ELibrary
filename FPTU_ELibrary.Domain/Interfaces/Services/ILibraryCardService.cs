using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryCardService<TDto> : IGenericService<LibraryCard, TDto, Guid>
    where TDto : class
{
    Task<IServiceResult> RegisterCardAsync(string email, TDto dto);
    Task<IServiceResult> RegisterCardByEmployeeAsync(string processedByEmail, 
        Guid userId, TDto dto, TransactionMethod method, int? paymentMethodId, int libraryCardPackageId);
    Task<IServiceResult> SendRequireToConfirmCardAsync(string userEmail);
    Task<IServiceResult> ConfirmCardRegisterWithoutSaveChangesAsync(string email, string transactionToken);
    Task<IServiceResult> ConfirmCardExtensionWithoutSaveChangesAsync(string email, string transactionToken);
    Task<IServiceResult> ConfirmCardAsync(Guid libraryCardId);
    Task<IServiceResult> ExtendCardAsync(Guid libraryCardId, 
        string? transactionToken, int? libraryCardPackageId, int? paymentMethodId);
    Task<IServiceResult> RejectCardAsync(Guid libraryCardId, string rejectReason);
    Task<IServiceResult> CheckCardExtensionAsync(Guid libraryCardId);
    Task<IServiceResult> CheckCardValidityAsync(Guid libraryCardId);
    Task<IServiceResult> UpdateBorrowMoreStatusWithoutSaveChangesAsync(Guid libraryCardId);
    Task<IServiceResult> ExtendBorrowAmountAsync(Guid libraryCardId, int maxItemOnceTime, string reason);
    Task<IServiceResult> SuspendCardAsync(Guid libraryCardId, DateTime suspensionEndDate, string reason);
    Task<IServiceResult> UnsuspendCardAsync(Guid libraryCardId);
    Task<IServiceResult> ArchiveCardAsync(Guid userId, Guid libraryCardId, string archiveReason);
    Task<IServiceResult> DeleteCardWithoutSaveChangesAsync(Guid libraryCardId);
    Task<IServiceResult> DeleteRangeCardWithoutSaveChangesAsync(Guid[] libraryCardIds);
}