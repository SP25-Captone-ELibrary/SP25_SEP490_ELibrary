using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Payment;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PayOSCancelPaymentRequest = FPTU_ELibrary.API.Payloads.Requests.Payment.PayOSCancelPaymentRequest;

namespace FPTU_ELibrary.API.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IInvoiceService<InvoiceDto> _invoiceService;
    private readonly ITransactionService<TransactionDto> _transactionService;

    public PaymentController(IInvoiceService<InvoiceDto> invoiceService,
        ITransactionService<TransactionDto>transactionService)
    {
        _invoiceService = invoiceService;
        _transactionService = transactionService;
    }

    [HttpPost(APIRoute.Payment.CreateTransactionDetails, Name = nameof(CreateTransactionDetails))]
    public async Task<IActionResult> CreateTransactionDetails([FromBody] TransactionDto req)
    {
        return Ok(await _transactionService.CreateAsync(req));
    }
    [HttpPost(APIRoute.Payment.CreatePayment, Name = nameof(CreatePayment))]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _invoiceService.CreatePayment(req.TransactionIds, email));
    }
    [HttpGet(APIRoute.Payment.GetPayOsPaymentLinkInformation, Name = nameof(GetPayOsPaymentLinkInformation))]
    public async Task<IActionResult> GetPayOsPaymentLinkInformation([FromRoute] string paymentLinkId)
    {
        return Ok(await _invoiceService.GetLinkInformationAsync(paymentLinkId));
    }
    [HttpPost(APIRoute.Payment.CancelPayment, Name = nameof(CancelPayment))]
    public async Task<IActionResult> CancelPayment([FromRoute] string paymentLinkId,[FromBody] PayOSCancelPaymentRequest req)
    {
        return Ok(await _invoiceService.CancelPayOsPaymentAsync(paymentLinkId,req.CancellationReason));
    }
    [HttpPost(APIRoute.Payment.VerifyPayment, Name = nameof(VerifyPayment))]
    public async Task<IActionResult> VerifyPayment ([FromBody]PayOSPaymentLinkInformationResponse req)
    {
        return Ok(await _invoiceService.VerifyPaymentWebhookDataAsync(req));
    }
}