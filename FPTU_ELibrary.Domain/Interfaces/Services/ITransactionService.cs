using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ITransactionService<TDto> : IGenericService<Transaction, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllCardHolderTransactionByUserIdAsync(Guid userId, int pageIndex ,int pageSize);
    Task<IServiceResult> GetCardHolderTransactionByIdAsync(Guid userId, int transactionId);
}