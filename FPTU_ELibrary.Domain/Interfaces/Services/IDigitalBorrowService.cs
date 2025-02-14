using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IDigitalBorrowService<TDto> : IGenericService<DigitalBorrow, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<IServiceResult> GetAllByEmailAsync(string email, int pageIndex, int pageSize);
}