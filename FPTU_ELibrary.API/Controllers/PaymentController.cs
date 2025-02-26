using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Transaction;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOSCancelPaymentRequest = FPTU_ELibrary.API.Payloads.Requests.Payment.PayOSCancelPaymentRequest;

namespace FPTU_ELibrary.API.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IPayOsService _payOsService;
    
    private readonly ITransactionService<TransactionDto> _transactionService;

    public PaymentController(
        IPayOsService payOsService,
        ITransactionService<TransactionDto>transactionService)
    {
        _payOsService = payOsService;
        _transactionService = transactionService;
    }
    
    [Authorize]
    [HttpPost(APIRoute.Payment.CreateTransaction, Name = nameof(CreateTransactionAsync))]
    public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionService.CreateAsync(dto: req.ToTransactionDto(), createdByEmail: email ?? string.Empty));
    }
    
    // [HttpPost(APIRoute.Payment.CreatePayment, Name = nameof(CreatePaymentAsync))]
    // public async Task<IActionResult> CreatePaymentAsync([FromBody] CreatePaymentRequest req)
    // {
    //     var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    //     return Ok(await _invoiceService.CreatePayment(req.TransactionIds, email ?? string.Empty));
    // }
    
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
}