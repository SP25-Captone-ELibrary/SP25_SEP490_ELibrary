using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBookEditionAuthorService<TDto> : IGenericService<BookEditionAuthor, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AddAuthorToBookEditionAsync(int bookEditionId, int authorId);
    Task<IServiceResult> AddRangeAuthorToBookEditionAsync(int bookEditionId, int[] authorIds);
    Task<IServiceResult> DeleteAuthorFromBookEditionAsync(int bookEditionId, int authorId);
    Task<IServiceResult> DeleteRangeAuthorFromBookEditionAsync(int bookEditionId, int[] authorIds);
    Task<IServiceResult> DeleteRangeWithoutSaveChangesAsync(int[] bookEditionAuthorIds);
}