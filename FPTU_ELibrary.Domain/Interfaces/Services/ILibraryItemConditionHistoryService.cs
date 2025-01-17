using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemConditionHistoryService<TDto> : IGenericService<LibraryItemConditionHistory, TDto, int>
    where TDto : class
{
    Task<IServiceResult> DeleteWithoutSaveChangesAsync(int id);
}