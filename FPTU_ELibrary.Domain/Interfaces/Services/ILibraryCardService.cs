using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryCardService<TDto> : IGenericService<LibraryCard, TDto, Guid>
    where TDto : class
{
    Task<IServiceResult> ConfirmRegisterAsync(Guid libraryCardId, string transactionToken);
    Task<IServiceResult> ConfirmExtendCardAsync(Guid libraryCardId, string transactionToken);
    Task<IServiceResult> CheckCardExtensionAsync(Guid libraryCardId);
    Task<IServiceResult> CheckCardValidityAsync(Guid libraryCardId);
    Task<IServiceResult> UpdateBorrowMoreStatusWithoutSaveChangesAsync(Guid libraryCardId);
    Task<IServiceResult> SuspendCardAsync(Guid libraryCardId, DateTime suspensionEndDate);
    Task<IServiceResult> UnsuspendCardAsync(Guid libraryCardId);
    Task<IServiceResult> ArchiveCardAsync(Guid userId, Guid libraryCardId, string archiveReason);
}