using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBookEditionInventoryService<TDto> : IGenericService<BookEditionInventory, TDto, int>
    where TDto : class
{
    Task<IServiceResult> UpdateWithoutSaveChangesAsync(TDto dto);
}