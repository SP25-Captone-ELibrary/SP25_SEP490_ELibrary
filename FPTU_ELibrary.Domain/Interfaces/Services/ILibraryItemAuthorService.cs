using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemAuthorService<TDto> : IGenericService<LibraryItemAuthor, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetFirstByLibraryItemIdAsync(int libraryItemId);
    Task<IServiceResult> AddAuthorToLibraryItemAsync(int libraryItemId, int authorId);
    Task<IServiceResult> AddRangeAuthorToLibraryItemAsync(int libraryItemId, int[] authorIds);
    Task<IServiceResult> DeleteAuthorFromLibraryItemAsync(int libraryItemId, int authorId);
    Task<IServiceResult> DeleteRangeAuthorFromLibraryItemAsync(int libraryItemId, int[] authorIds);
    Task<IServiceResult> DeleteRangeWithoutSaveChangesAsync(int[] bookEditionAuthorIds);
}