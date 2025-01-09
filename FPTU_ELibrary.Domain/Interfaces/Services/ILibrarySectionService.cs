using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibrarySectionService<TDto> : IGenericService<LibrarySection, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllByZoneIdAsync(int zoneId);
}

