using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class DigitalBorrowController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;

    public DigitalBorrowController(
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _digitalBorrowSvc = digitalBorrowSvc;
    }

    [Authorize]
    [HttpGet(APIRoute.DigitalBorrow.GetAll, Name = nameof(GetAllDigitalBorrowAsync))]
    public async Task<IActionResult> GetAllDigitalBorrowAsync([FromQuery] DigitalBorrowSpecParams specParams)
    {
        return Ok(await _digitalBorrowSvc.GetAllWithSpecAsync(new DigitalBorrowSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.DigitalBorrow.GetById, Name = nameof(GetDigitalBorrowByIdAsync))]
    public async Task<IActionResult> GetDigitalBorrowByIdAsync([FromRoute] int id)
    {
        return Ok(await _digitalBorrowSvc.GetByIdAsync(id, email: null, userId: null, isCallFromManagement: true));
    }
}