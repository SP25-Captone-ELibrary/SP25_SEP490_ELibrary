using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ITransactionService<TDto> : IGenericService<Transaction, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateAsync(TDto dto, string createdByEmail);
    Task<IServiceResult> CreateTransactionForBorrowRequestAsync(string createdByEmail, int borrowRequestId);
    Task<IServiceResult> CreateTransactionForBorrowRecordAsync(string createdByEmail, int borrowRecordId);
    Task<IServiceResult> CreateWithoutSaveChangesAsync(TDto dto);
    Task<IServiceResult> GetAllByTransactionCodeAsync(string transactionCode);
    Task<IServiceResult> GetAllCardHolderTransactionAsync(ISpecification<Transaction> spec, bool tracked = false);
    Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<Transaction> spec);
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null);
    Task<IServiceResult> UpdateStatusByTransactionCodeAsync(
        string transactionCode, DateTime? transactionDate,
        string? cancellationReason, DateTime? cancelledAt, TransactionStatus status);
    Task<IServiceResult> CancelTransactionsByCodeAsync(string transactionCode, string cancellationReason);
}