using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class FineController: ControllerBase
{
    private readonly IFineService<FineDto> _fineService;

    public FineController(ITransactionService<TransactionDto>transactionService,
        IFineService<FineDto>fineService)
    {
        _fineService = fineService;
    }

    // [HttpPost(APIRoute.Fine.Create, Name = nameof(CreateFineWithBorrowRecord))]
    // [Authorize]
    // public async Task<IActionResult> CreateFineWithBorrowRecord([FromBody] CreateFineRequest req)
    // {
    //     var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    //     return Ok(await _fineService.CreateFineForBorrowRecord(req.FinePolicyId, req.BorrowRecordId,email));
    // }
}