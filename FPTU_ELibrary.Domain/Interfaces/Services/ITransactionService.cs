using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ITransactionService<TDto> : IGenericService<Transaction, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateAsync(TDto dto, string createdByEmail);
    Task<IServiceResult> CreateWithoutSaveChangesAsync(TDto dto);
    Task<IServiceResult> GetAllByTransactionCodeAsync(string transactionCode);
    Task<IServiceResult> GetAllCardHolderTransactionByUserIdAsync(Guid userId, int pageIndex ,int pageSize);
    Task<IServiceResult> GetCardHolderTransactionByIdAsync(Guid userId, int transactionId);
    Task<IServiceResult> UpdateStatusByTransactionCodeAsync(
        string transactionCode, DateTime? transactionDate,
        string? cancellationReason, DateTime? cancelledAt, TransactionStatus status);
    Task<IServiceResult> CancelTransactionsByCodeAsync(string transactionCode, string cancellationReason);
}