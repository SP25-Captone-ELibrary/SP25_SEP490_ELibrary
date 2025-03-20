using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemConditionHistoryService<TDto> : IGenericService<LibraryItemConditionHistory, TDto, int>
    where TDto : class
{
    Task<IServiceResult> UpdateByConditionIdWithoutSaveChangesAsync(int libraryItemInstanceId, int conditionId);
    Task<IServiceResult> UpdateRangeConditionWithoutSaveChangesAsync(List<int> libraryItemInstanceIds, LibraryItemConditionStatus status);
    Task<IServiceResult> DeleteWithoutSaveChangesAsync(int id);
}