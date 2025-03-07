using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryFloorService<TDto> : IGenericService<LibraryFloor, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetMapByFloorIdAsync(int floorId);
}