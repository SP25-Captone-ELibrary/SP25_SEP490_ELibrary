using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IPayOsService
{
    Task<IServiceResult> GetLinkInformationAsync(string paymentLinkId);
    Task<IServiceResult> VerifyPaymentWebhookDataAsync(PayOSPaymentLinkInformationResponseDto req);
    Task<IServiceResult> CancelPaymentAsync(string paymentLinkId, string orderCode, string cancellationReason);
}