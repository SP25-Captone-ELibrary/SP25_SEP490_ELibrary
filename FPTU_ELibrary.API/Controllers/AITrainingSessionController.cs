using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class AITrainingSessionController : ControllerBase
{
    private readonly IAITrainingSessionService<AITrainingSessionDto> _trainingSessionService;

    public AITrainingSessionController(
        IAITrainingSessionService<AITrainingSessionDto> trainingSessionService)
    {
        _trainingSessionService = trainingSessionService;
    }

    [HttpGet(APIRoute.AITrainingSession.GetAll, Name = nameof(GetAll))]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] AITrainingSessionSpecParams specParams)
    {
        return Ok(await _trainingSessionService.GetAllWithSpecAsync(new AITrainingSessionSpecification(
            aiTrainingSessionSpecParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? 10)));
    }

    [HttpGet(APIRoute.AITrainingSession.GetById, Name = nameof(GetById))]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        return Ok(await _trainingSessionService.GetByIdAsync(id));
    }
}