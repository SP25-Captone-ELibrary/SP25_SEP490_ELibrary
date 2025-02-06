using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryCardService<TDto> : IGenericService<LibraryCard, TDto, Guid>
    where TDto : class
{
    Task<IServiceResult> CheckCardValidityAsync(Guid libraryCardId);
    Task<IServiceResult> UpdateBorrowMoreStatusWithoutSaveChangesAsync(Guid libraryCardId);
}