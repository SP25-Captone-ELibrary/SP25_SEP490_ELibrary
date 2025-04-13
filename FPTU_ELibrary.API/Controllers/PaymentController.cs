using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Payment;
using FPTU_ELibrary.API.Payloads.Requests.Transaction;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using ILogger = Serilog.ILogger;
using PayOSCancelPaymentRequest = FPTU_ELibrary.API.Payloads.Requests.Payment.PayOSCancelPaymentRequest;

namespace FPTU_ELibrary.API.Controllers;

public class PaymentController : ControllerBase
{
    private readonly AppSettings _appSettings;
    
    private readonly IPayOsService _payOsService;
    private readonly ITransactionService<TransactionDto> _transactionService;

    private readonly ILogger _logger;
    private readonly PayOSSettings _payOsSettings;

    public PaymentController(
        ILogger logger,
        IPayOsService payOsService,
        ITransactionService<TransactionDto>transactionService,
        IOptionsMonitor<AppSettings> monitor,
        IOptionsMonitor<PayOSSettings> monitor1)
    {
        _logger = logger;
        _payOsService = payOsService;
        _transactionService = transactionService;
        _appSettings = monitor.CurrentValue;
        _payOsSettings = monitor1.CurrentValue;
    }

    #region Management
    [Authorize]
    [HttpGet(APIRoute.Payment.GetAllTransaction, Name = nameof(GetAllTransaction))]
    public async Task<IActionResult> GetAllTransaction([FromQuery] TransactionSpecParams specParams)
    {
        return Ok(await _transactionService.GetAllCardHolderTransactionAsync(new TransactionSpecification(
            specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [Authorize]
    [HttpGet(APIRoute.Payment.GetTransactionById, Name = nameof(GetTransactionByIdAsync))]
    public async Task<IActionResult> GetTransactionByIdAsync([FromRoute] int id)
    {
        return Ok(await _transactionService.GetByIdAsync(id: id, email: null, userId: null));
    }
    #endregion
    
    [Authorize]
    [HttpGet(APIRoute.Payment.GetPrivacyTransaction, Name = nameof(GetPrivacyTransactionAsync))]
    public async Task<IActionResult> GetPrivacyTransactionAsync([FromQuery] TransactionSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.GetAllWithSpecAsync(new TransactionSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize,
            email: email), tracked: false));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CreateTransaction, Name = nameof(CreateTransactionAsync))]
    public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.CreateAsync(dto: req.ToTransactionDto(), createdByEmail: email ?? string.Empty));
    }

    [Authorize]
    [HttpPost(APIRoute.Payment.CreateBorrowRecordTransaction, Name = nameof(CreateBorrowRecordTransactionAsync))]
    public async Task<IActionResult> CreateBorrowRecordTransactionAsync([FromRoute] int borrowRecordId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.CreateTransactionForBorrowRecordAsync(
            createdByEmail: email ?? string.Empty,
            borrowRecordId: borrowRecordId));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CreateBorrowRequestTransaction, Name = nameof(CreateBorrowRequestTransactionAsync))]
    public async Task<IActionResult> CreateBorrowRequestTransactionAsync([FromRoute] int borrowRequestId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.CreateTransactionForBorrowRequestAsync(
            createdByEmail: email ?? string.Empty,
            borrowRequestId: borrowRequestId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Payment.GetPayOsPaymentLinkInformation, Name = nameof(GetPayOsPaymentLinkInformationAsync))]
    public async Task<IActionResult> GetPayOsPaymentLinkInformationAsync([FromRoute] string paymentLinkId)
    {
        return Ok(await _payOsService.GetLinkInformationAsync(paymentLinkId));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CancelPayment, Name = nameof(CancelPaymentAsync))]
    public async Task<IActionResult> CancelPaymentAsync([FromRoute] string paymentLinkId, [FromBody] PayOSCancelPaymentRequest req)
    {
        return Ok(await _payOsService.CancelPaymentAsync(
            paymentLinkId: paymentLinkId, 
            orderCode: req.OrderCode,
            cancellationReason: req.CancellationReason));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.VerifyPayment, Name = nameof(VerifyPaymentAsync))]
    public async Task<IActionResult> VerifyPaymentAsync ([FromBody] PayOSPaymentLinkInformationResponseDto req)
    {
        return Ok(await _payOsService.VerifyPaymentWebhookDataAsync(req));
    }

    [HttpPost(APIRoute.Payment.WebhookPayOsReturn, Name = nameof(WebhookPayOsReturnAsync))]
    public async Task<IActionResult> WebhookPayOsReturnAsync([FromBody] WebhookType req)
    {
        return Ok(await _payOsService.VerifyPaymentWebhookDataAsync(req.ToPayOsResponseDto(TransactionStatus.Paid)));
    }
    
    [HttpPost(APIRoute.Payment.SendWebhookConfirm, Name = nameof(SendWebhookConfirmAsync))]
    public async Task<IActionResult> SendWebhookConfirmAsync([FromBody] SendWebhookConfirmRequest req)
    {
        PayOS payOs = new PayOS(_payOsSettings.ClientId, _payOsSettings.ApiKey, _payOsSettings.ChecksumKey);
        return Ok(await payOs.confirmWebhook(req.WebhookUrl));
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