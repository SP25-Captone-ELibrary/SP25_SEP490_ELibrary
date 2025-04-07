using FPTU_ELibrary.Application.Dtos.Dashboard;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IDashboardService
{
    Task<IServiceResult> GetDashboardOverviewAsync(DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetDashboardCirculationAndBorrowingAnalyticsAsync(DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetDashboardDigitalResourceAnalyticsAsync(LibraryResourceSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetDashboardFinancialAndTransactionAnalyticsAsync(TransactionSpecification spec,
        DateTime? startDate, DateTime? endDate,
        TrendPeriod period, TransactionType? transactionType);
    Task<IServiceResult> GetAllOverdueBorrowAsync(BorrowRecordDetailSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetLatestBorrowAsync(BorrowRecordDetailSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetAssignableReservationAsync(ReservationQueueSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period);
    Task<IServiceResult> GetTopCirculationItemsAsync(TopCirculationItemSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period); // Recommend for potential items to buy
}