using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemConditionController : ControllerBase
{
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionSvc;

    public LibraryItemConditionController(
        ILibraryItemConditionService<LibraryItemConditionDto> conditionSvc)
    {
        _conditionSvc = conditionSvc;
    }
    
    #region Management
    [Authorize]
    [HttpGet(APIRoute.LibraryItemCondition.GetAll, Name = nameof(GetAllLibraryItemConditionAsync))]
    public async Task<IActionResult> GetAllLibraryItemConditionAsync()
    {
        return Ok(await _conditionSvc.GetAllAsync());
    }
    #endregion
}