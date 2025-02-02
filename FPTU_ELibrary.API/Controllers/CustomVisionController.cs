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
    [HttpPost(APIRoute.Group.CheckAvailableGroup,Name = nameof(CheckAvailableGroup))]
    public async Task<IActionResult> CheckAvailableGroup([FromBody] CheckAvailableGroupRequest req)
    {
        return Ok(await _aiClassificationService.IsAbleToCreateGroup(req.RootItemId,req.OtherItemIds));
    } 
    [HttpPost(APIRoute.Group.DefineGroup,Name = nameof(DefineGroup))]
    public async Task<IActionResult> DefineGroup([FromBody]CheckAvailableGroupRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _aiClassificationService.GetAvailableGroup(email,req.RootItemId,req.OtherItemIds));
    }
    [HttpPost(APIRoute.AIServices.TrainingAfterCreate, Name = nameof(TrainModelAfterCreate))]
    [Authorize]
    public async Task<IActionResult> TrainModelAfterCreate([FromForm] TrainModelAfterCreateRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _aiClassificationService.TrainModel(req.BookCode,req.ImageList,email));
    }
    //
    // [HttpPost(APIRoute.BookEdition.Training, Name = nameof(TrainingSingleEdition))]
    // [Authorize]
    // public async Task<IActionResult> TrainingSingleEdition([FromForm] BaseTrainedModelRequest req,[FromRoute] int id)
    // {
    //     var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    //     return Ok(await _aiClassificationService.TrainModelWithoutCreate(id,req.ImageList,email));
    // }
    //
    // [HttpPost(APIRoute.AIServices.Predict, Name = nameof(Predict))]
    // public async Task<IActionResult> Predict([FromForm] PredictRequest req)
    // {
    //     return Ok(await _aiClassificationService.PredictAsync(req.ImageToPredict));
    // }
    //
    // [HttpPost(APIRoute.AIServices.Recommendation, Name = nameof(Recommendation))]
    // public async Task<IActionResult> Recommendation([FromForm] PredictRequest req)
    // {
    //     return Ok(await _aiClassificationService.Recommendation(req.ImageToPredict));
    // }
}