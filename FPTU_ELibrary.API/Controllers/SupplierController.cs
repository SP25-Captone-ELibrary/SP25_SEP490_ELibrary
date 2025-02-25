using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Supplier;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService<SupplierDto> _supplierService;
    private readonly AppSettings _appSettings;

    public SupplierController(
        ISupplierService<SupplierDto> supplierService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _supplierService = supplierService;
        _appSettings = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpPost(APIRoute.Supplier.Create, Name = nameof(CreateSupplierAsync))]
    public async Task<IActionResult> CreateSupplierAsync([FromBody] CreateSupplierRequest req)
    {
        return Ok(await _supplierService.CreateAsync(req.ToSupplierDto()));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Supplier.Import, Name = nameof(ImportSupplierAsync))]
    public async Task<IActionResult> ImportSupplierAsync([FromForm] ImportSupplierRequest req)
    {
        return Ok(await _supplierService.ImportAsync(
            file: req.File,
            scanningFields: req.ScanningFields,
            duplicateHandle: req.DuplicateHandle));
    }

    [Authorize]
    [HttpGet(APIRoute.Supplier.GetAll, Name = nameof(GetAllSupplierAsync))]
    public async Task<IActionResult> GetAllSupplierAsync([FromQuery] SupplierSpecParams specParams)
    {
        return Ok(await _supplierService.GetAllWithSpecAsync(new SupplierSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.Supplier.GetById, Name = nameof(GetSupplierByIdAsync))]
    public async Task<IActionResult> GetSupplierByIdAsync([FromRoute] int id)
    {
        return Ok(await _supplierService.GetByIdAsync(id));
    }

    // [Authorize]
    [HttpGet(APIRoute.Supplier.Export, Name = nameof(ExportSupplierAsync))]
    public async Task<IActionResult> ExportSupplierAsync([FromQuery] SupplierSpecParams specParams)
    {
        var exportResult = await _supplierService.ExportAsync(new SupplierSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));
    
        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "suppliers.xlsx")
            : Ok(exportResult);
    }
    
    [Authorize]
    [HttpPut(APIRoute.Supplier.Update, Name = nameof(UpdateSupplierAsync))]
    public async Task<IActionResult> UpdateSupplierAsync([FromRoute] int id, [FromBody] UpdateSupplierRequest req)
    {
        return Ok(await _supplierService.UpdateAsync(id, req.ToSupplierDto()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Supplier.Delete, Name = nameof(DeleteSupplierByIdAsync))]
    public async Task<IActionResult> DeleteSupplierByIdAsync([FromRoute] int id)
    {
        return Ok(await _supplierService.DeleteAsync(id));
    }
}