using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuthorService<TDto> : IGenericService<Author, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAuthorDetailByIdAsync(int id);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
}