using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IInvoiceService<TDto> : IGenericService<Invoice, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreatePayment(List<int> transactionIds, string email);
    Task<IServiceResult> GetLinkInformationAsync(string paymentLinkId);
    Task<IServiceResult> CancelPayOsPaymentAsync(string paymentLinkId,string cancellationReason);
    Task<IServiceResult> GetAllCardHolderInvoiceByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<IServiceResult> GetCardHolderInvoiceByIdAsync(Guid userId, int invoiceId);
}