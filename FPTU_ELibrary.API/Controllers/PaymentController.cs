using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Transaction;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PayOSCancelPaymentRequest = FPTU_ELibrary.API.Payloads.Requests.Payment.PayOSCancelPaymentRequest;

namespace FPTU_ELibrary.API.Controllers;

public class PaymentController : ControllerBase
{
    private readonly AppSettings _appSettings;
    
    private readonly IPayOsService _payOsService;
    private readonly ITransactionService<TransactionDto> _transactionService;

    public PaymentController(
        IPayOsService payOsService,
        ITransactionService<TransactionDto>transactionService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _payOsService = payOsService;
        _transactionService = transactionService;
        _appSettings = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CreateTransaction, Name = nameof(CreateTransactionAsync))]
    public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.CreateAsync(dto: req.ToTransactionDto(), createdByEmail: email ?? string.Empty));
    }
        
    [Authorize]
    [HttpGet(APIRoute.Payment.GetPayOsPaymentLinkInformation, Name = nameof(GetPayOsPaymentLinkInformation))]
    public async Task<IActionResult> GetPayOsPaymentLinkInformation([FromRoute] string paymentLinkId)
    {
        return Ok(await _payOsService.GetLinkInformationAsync(paymentLinkId));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CancelPayment, Name = nameof(CancelPayment))]
    public async Task<IActionResult> CancelPayment([FromRoute] string paymentLinkId,[FromBody] PayOSCancelPaymentRequest req)
    {
        return Ok(await _payOsService.CancelPaymentAsync(
            paymentLinkId: paymentLinkId, 
            orderCode: req.OrderCode,
            cancellationReason: req.CancellationReason));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.VerifyPayment, Name = nameof(VerifyPayment))]
    public async Task<IActionResult> VerifyPayment ([FromBody] PayOSPaymentLinkInformationResponseDto req)
    {
        return Ok(await _payOsService.VerifyPaymentWebhookDataAsync(req));
    }

    [Authorize]
    [HttpGet(APIRoute.Payment.GetAllTransaction, Name = nameof(GetAllTransaction))]
    public async Task<IActionResult> GetAllTransaction([FromQuery] TransactionSpecParams specParams)
    {
        return Ok(await _transactionService.GetAllWithSpecAsync(new TransactionSpecification(
            specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Payment.GetPrivacyTransaction, Name = nameof(GetOwnTransaction))]
    public async Task<IActionResult> GetOwnTransaction([FromQuery] TransactionSpecParams specParams)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _transactionService.GetAllWithSpecAsync(new TransactionSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize,
            email: email), tracked: false));
    }

    #region Archived Code
    // [HttpPost(APIRoute.Payment.CreatePayment, Name = nameof(CreatePaymentAsync))]
    // public async Task<IActionResult> CreatePaymentAsync([FromBody] CreatePaymentRequest req)
    // {
    //     var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    //     return Ok(await _invoiceService.CreatePayment(req.TransactionIds, email ?? string.Empty));
    // }
    #endregion
}