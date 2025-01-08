using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBookEditionService<TDto> : IGenericService<BookEdition, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateAsync(int bookId, TDto dto);
    Task<IServiceResult> GetDetailAsync(int id);
    Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
    Task<IServiceResult> UpdateTrainingStatusAsync(Guid trainingBookCode);
    Task<IServiceResult> GetRelatedEditionWithMatchField(TDto dto, string fieldName);
}