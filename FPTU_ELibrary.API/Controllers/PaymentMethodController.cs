using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class PaymentMethodController : ControllerBase
{
    private readonly IPaymentMethodService<PaymentMethodDto> _paymentMethodService;

    public PaymentMethodController(IPaymentMethodService<PaymentMethodDto> paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }
    
    [HttpGet(APIRoute.PaymentMethod.GetAll, Name = nameof(GetAllPaymentMethodAsync))]
    public async Task<IActionResult> GetAllPaymentMethodAsync()
    {
        return Ok(await _paymentMethodService.GetAllAsync());
    }
}