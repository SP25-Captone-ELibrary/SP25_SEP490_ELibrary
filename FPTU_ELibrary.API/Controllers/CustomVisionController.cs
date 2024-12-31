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

    [HttpPost(APIRoute.AIServices.Training, Name = nameof(TrainModel))]
    [Authorize]
    public async Task<IActionResult> TrainModel([FromForm] TrainModelRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _aiClassificationService.TrainModel(req.BookId, req.ImageList,email));
    }
}