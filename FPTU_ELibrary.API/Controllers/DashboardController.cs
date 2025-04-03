using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Dashboard;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Dashboard;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class DashboardController : ControllerBase 
{
    private readonly IDashboardService _dashboardSvc;
    private readonly AppSettings _appSettings;

    public DashboardController(
        IDashboardService dashboardSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _dashboardSvc = dashboardSvc;
    }
    
    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetDashboardOverview, Name = nameof(GetDashboardOverviewAsync))]
    public async Task<IActionResult> GetDashboardOverviewAsync([FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetDashboardOverviewAsync(
            startDate: req.StartDate,
            endDate: req.EndDate,
            period: req.Period));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetDashboardCirculationAnalyst, Name = nameof(GetDashboardCirculationAnalystAsync))]
    public async Task<IActionResult> GetDashboardCirculationAnalystAsync(
        [FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetDashboardCirculationAndBorrowingAnalyticsAsync(
            startDate: req.StartDate,
            endDate: req.EndDate,
            period: req.Period));
    }

    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetDashboardDigitalResourceAnalyst, Name = nameof(GetDashboardDigitalResourceAnalystAsync))]
    public async Task<IActionResult> GetDashboardDigitalResourceAnalystAsync(
        [FromQuery] LibraryResourceSpecParams specParams,
        [FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetDashboardDigitalResourceAnalyticsAsync(
            spec: new LibraryResourceSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            startDate: req.StartDate,
            endDate: req.EndDate,
            period: req.Period));
    }

    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetDashboardFinancialAndTransactionAnalyst, Name = nameof(GetDashboardFinancialAndTransactionAnalystAsync))]
    public async Task<IActionResult> GetDashboardFinancialAndTransactionAnalystAsync(
        [FromQuery] TransactionSpecParams specParams,
        [FromQuery] DashboardFilterRequest req,
        [FromQuery] TransactionType? transactionType = null)
    {
        return Ok(await _dashboardSvc.GetDashboardFinancialAndTransactionAnalyticsAsync(
            spec: new TransactionSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            transactionType: transactionType,
            startDate: req.StartDate,
            endDate: req.EndDate,
            period: req.Period));
    }

    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetAllOverdueBorrow, Name = nameof(GetAllOverdueBorrowAsync))]
    public async Task<IActionResult> GetAllOverdueBorrowAsync(
        [FromQuery] BorrowRecordDetailSpecParams specParams,
        [FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetAllOverdueBorrowAsync(
            new BorrowRecordDetailSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            startDate: req.StartDate,
            endDate: req.EndDate
        ));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetAllLatestBorrow, Name = nameof(GetAllLatestBorrowAsync))]
    public async Task<IActionResult> GetAllLatestBorrowAsync(
        [FromQuery] BorrowRecordDetailSpecParams specParams,
        [FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetLatestBorrowAsync(
            new BorrowRecordDetailSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            startDate: req.StartDate,
            endDate: req.EndDate
        ));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Dashboard.GetAllAssignableReservation, Name = nameof(GetAllAssignableReservationAsync))]
    public async Task<IActionResult> GetAllAssignableReservationAsync(
        [FromQuery] ReservationQueueSpecParams specParams,
        [FromQuery] DashboardFilterRequest req)
    {
        return Ok(await _dashboardSvc.GetAssignableReservationAsync(
            new ReservationQueueSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            startDate: req.StartDate,
            endDate: req.EndDate
        ));
    }
}