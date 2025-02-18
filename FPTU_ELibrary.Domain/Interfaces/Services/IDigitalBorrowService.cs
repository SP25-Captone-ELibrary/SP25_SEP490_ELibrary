using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IDigitalBorrowService<TDto> : IGenericService<DigitalBorrow, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllCardHolderDigitalBorrowByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<IServiceResult> GetCardHolderDigitalBorrowByIdAsync(Guid userId, int digitalBorrowId);
}