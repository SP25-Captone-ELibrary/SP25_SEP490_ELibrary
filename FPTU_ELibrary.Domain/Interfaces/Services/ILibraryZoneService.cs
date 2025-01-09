using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryZoneService<TDto> : IGenericService<LibraryZone, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllByFloorIdAsync(int floorId);
}