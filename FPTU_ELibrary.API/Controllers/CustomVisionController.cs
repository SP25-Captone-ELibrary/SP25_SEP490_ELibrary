using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.CustomVision;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class CustomVisionController : ControllerBase
{
    private readonly IAIClassificationService _aiClassificationService;

    public CustomVisionController(IAIClassificationService aiClassificationService)
    {
        _aiClassificationService = aiClassificationService;
    }

    // [HttpPost(APIRoute.AIServices.Training, Name = nameof(TrainModel))]
    // [Authorize]
    // public async Task<IActionResult> TrainModel([FromForm] List<TrainedModelRequest> req)
    // {
    //     var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    //     return Ok(await _aiClassificationService.TrainModel(req.ToListTrainedBookDetailDto(),email));
    // }
    
    [HttpPost(APIRoute.AIServices.TrainingAfterCreate, Name = nameof(TrainModelAfterCreate))]
    [Authorize]
    public async Task<IActionResult> TrainModelAfterCreate([FromForm] TrainModelAfterCreateRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _aiClassificationService.TrainModelAfterCreate(req.BookCode,req.ImageList,email));
    }
}