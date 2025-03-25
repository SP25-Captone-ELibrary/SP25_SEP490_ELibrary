using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;
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

    [Authorize]
    [HttpGet(APIRoute.LibraryItemCondition.GetAllForStockInWarehouse, Name = nameof(GetAllItemConditionForStockInWarehouse))]
    public async Task<IActionResult> GetAllItemConditionForStockInWarehouse([FromQuery] TrackingType trackingType)
    {
        return Ok(await _conditionSvc.GetAllForStockInWarehouseAsync(trackingType: trackingType));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemCondition.GetById, Name = nameof(GetLibraryItemConditionByIdAsync))]
    public async Task<IActionResult> GetLibraryItemConditionByIdAsync([FromRoute] int id)
    {
        return Ok(await _conditionSvc.GetByIdAsync(id));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItemCondition.Create, Name = nameof(CreateLibraryItemConditionAsync))]
    public async Task<IActionResult> CreateLibraryItemConditionAsync([FromRoute] int id, 
        [FromBody] CreateLibraryItemConditionRequest req)
    {
        return Ok(await _conditionSvc.CreateAsync(req.ToLibraryItemConditionDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryItemCondition.Update, Name = nameof(UpdateLibraryItemConditionAsync))]
    public async Task<IActionResult> UpdateLibraryItemConditionAsync([FromRoute] int id, 
        [FromBody] UpdateLibraryItemConditionRequest req)
    {
        return Ok(await _conditionSvc.UpdateAsync(id: id, req.ToLibraryItemConditionDto()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItemCondition.Delete, Name = nameof(DeleteLibraryItemConditionByIdAsync))]
    public async Task<IActionResult> DeleteLibraryItemConditionByIdAsync([FromRoute] int id)
    {
        return Ok(await _conditionSvc.DeleteAsync(id));
    }
    #endregion
}