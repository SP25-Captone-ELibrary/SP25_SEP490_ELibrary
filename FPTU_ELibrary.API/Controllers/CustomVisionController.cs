using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.CustomVision;
using FPTU_ELibrary.API.Payloads.Requests.Group;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace FPTU_ELibrary.API.Controllers;
    
public class CustomVisionController : ControllerBase
{
    private readonly IAIClassificationService _aiClassificationService;
    private readonly IAIDetectionService _aiDetectionService;

    public CustomVisionController(IAIClassificationService aiClassificationService
        ,IAIDetectionService aiDetectionService)
    {
        _aiClassificationService = aiClassificationService;
        _aiDetectionService = aiDetectionService;
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
    [HttpPost(APIRoute.AIServices.RawDetect, Name = nameof(RawDetect))]
    public async Task<IActionResult> RawDetect([FromForm] RawDetectRequest req,[FromRoute]int id)
    {
        return Ok(await _aiDetectionService.RawDetectAsync(req.ImageToDetect,id));
    }
    [HttpPost(APIRoute.AIServices.Predict, Name = nameof(Predict))]
    public async Task<IActionResult> Predict([FromForm] PredictRequest req)
    {
        return Ok(await _aiClassificationService.PredictAsync(req.ImageToPredict));
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
    [HttpPost(APIRoute.AIServices.RecommendationWithId, Name = nameof(RecommendationWithId))]
    public async Task<IActionResult> RecommendationWithId([FromRoute] int id)
    {
        return Ok(await _aiClassificationService.RecommendBook(id));
    }
    [HttpPost(APIRoute.AIServices.Recommendation, Name = nameof(Recommendation))]
    public async Task<IActionResult> Recommendation([FromForm] PredictRequest req)
    {
        return Ok(await _aiClassificationService.RecommendBook(req.ImageToPredict));
    }
    [HttpGet(APIRoute.Group.GetSuitableItemsForGrouping, Name = nameof(GetSuitableItemsForGrouping))]
    public async Task<IActionResult> GetSuitableItemsForGrouping([FromRoute] int rootItemId)
    {
        return Ok(await _aiClassificationService.GetAndGradeAllSuitableItemsForGrouping(rootItemId));
    }
    [HttpGet(APIRoute.Group.GroupedItems,Name = nameof(GroupedItems))]
    public async Task<IActionResult> GroupedItems([FromBody] GroupAllItemsRequest req)
    {
        return Ok(await _aiClassificationService.GetAndGradeAllSuitableItemsForGrouping(req.SelectedItemIds));
    }

    [HttpPost(APIRoute.AIServices.Training, Name = nameof(ExtendTrainingProgress))]
    [Authorize]
    public async Task<IActionResult> ExtendTrainingProgress([FromBody] ExtendTrainingProgressRequest req)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var reqBody = req.ToTrainingData();
        return Ok(await _aiClassificationService.ExtendModelTraining(reqBody.Item1,reqBody.Item2,email));
    }

}