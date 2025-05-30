using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly AppSettings _appSettings;

    public EmployeeController(
        IEmployeeService<EmployeeDto> employeeService,
        IOptionsMonitor<AppSettings> appSettings)
    {
        _employeeService = employeeService;
        _appSettings = appSettings.CurrentValue;
    }
    
    [Authorize]
    [HttpGet(APIRoute.Employee.GetAll, Name = nameof(GetAllEmployeeAsync))]
    public async Task<IActionResult> GetAllEmployeeAsync([FromQuery] EmployeeSpecParams specParams)
    {
        return Ok(await _employeeService.GetAllWithSpecAsync(new EmployeeSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [Authorize]
    [HttpGet(APIRoute.Employee.GetById, Name = nameof(GetEmployeeByIdAsync))]
    public async Task<IActionResult> GetEmployeeByIdAsync([FromRoute] Guid id)
    {
        return Ok(await _employeeService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Employee.Create, Name = nameof(CreateEmployeeAsync))]
    public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployeeRequest req)
    {
        return Ok(await _employeeService.CreateAsync(req.ToEmployeeDtoForCreate()));
    }

    [Authorize]
    [HttpPost(APIRoute.Employee.Import, Name = nameof(ImportEmployeeAsync))]
    public async Task<IActionResult> ImportEmployeeAsync([FromForm] ImportEmployeeRequest req)
    {
        return Ok(await _employeeService.ImportAsync(req.File, req.DuplicateHandle,
            req.ColumnSeparator,  req.EncodingType, req.ScanningFields));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Employee.Export, Name = nameof(ExportEmployeeAsync))]
    public async Task<IActionResult> ExportEmployeeAsync([FromQuery] EmployeeSpecParams specParams)
    {
        var exportResult = await _employeeService.ExportAsync(new EmployeeSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employees.xlsx")
            : Ok(exportResult);
    }
    
    [Authorize]
    [HttpPut(APIRoute.Employee.Update, Name = nameof(UpdateEmployeeAsync))]
    public async Task<IActionResult> UpdateEmployeeAsync([FromRoute] Guid id, [FromBody] UpdateRequest req)
    {
        return Ok(await _employeeService.UpdateAsync(id, req.ToEmployeeDtoForUpdate()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Employee.ChangeActiveStatus, Name = nameof(ChangeActiveStatusAsync))]
    public async Task<IActionResult> ChangeActiveStatusAsync([FromRoute] Guid id)
    {
        return Ok(await _employeeService.ChangeActiveStatusAsync(id));
    }
        
    [Authorize]
    [HttpPatch(APIRoute.Employee.SoftDelete, Name = nameof(SoftDeleteEmployeeAsync))]
    public async Task<IActionResult> SoftDeleteEmployeeAsync([FromRoute] Guid id)
    {
        return Ok(await _employeeService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Employee.SoftDeleteRange, Name = nameof(SoftDeleteEmployeeRangeAsync))]
    public async Task<IActionResult> SoftDeleteEmployeeRangeAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _employeeService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Employee.UndoDelete, Name = nameof(UndoDeleteEmployeeAsync))]
    public async Task<IActionResult> UndoDeleteEmployeeAsync([FromRoute] Guid id)
    {
        return Ok(await _employeeService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Employee.UndoDeleteRange, Name = nameof(UndoDeleteRangeEmployeeAsync))]
    public async Task<IActionResult> UndoDeleteRangeEmployeeAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _employeeService.UndoDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Employee.Delete, Name = nameof(DeleteEmployeeAsync))]
    public async Task<IActionResult> DeleteEmployeeAsync([FromRoute] Guid id)
    {
        return Ok(await _employeeService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Employee.DeleteRange, Name = nameof(DeleteRangeEmployeeAsync))]
    public async Task<IActionResult> DeleteRangeEmployeeAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _employeeService.DeleteRangeAsync(req.Ids));
    }
}