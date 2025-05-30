using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryClosureDayService<TDto> : IGenericService<LibraryClosureDay, TDto, int>
    where TDto : class
{
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
}