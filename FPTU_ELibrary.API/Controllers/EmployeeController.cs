using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
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
    
    [HttpGet(APIRoute.Employee.GetAll, Name = nameof(GetAllEmployeeAsync))]
    public async Task<IActionResult> GetAllEmployeeAsync([FromQuery] EmployeeSpecParams specParams)
    {
        return Ok(await _employeeService.GetAllWithSpecAsync(new EmployeeSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [HttpPost(APIRoute.Employee.Create, Name = nameof(CreateEmployeeAsync))]
    public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployeeRequest req)
    {
        return Ok(await _employeeService.CreateAsync(req.ToEmployeeDtoForCreate()));
    }
}