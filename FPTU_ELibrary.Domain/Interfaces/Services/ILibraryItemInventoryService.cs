using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemInventoryService<TDto> : IGenericService<LibraryItemInventory, TDto, int>
    where TDto : class
{
    Task<IServiceResult> UpdateWithoutSaveChangesAsync(TDto dto);
}