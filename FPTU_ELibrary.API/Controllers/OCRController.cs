using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.CustomVision;
using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class OCRController : ControllerBase
{
    private readonly IOCRService _ocrService;
    public OCRController(IOCRService ocrService)
    {
        _ocrService = ocrService;
    }

    // [Authorize]
    [HttpPost(APIRoute.AIServices.OCR,Name = nameof(CheckImagesWithDetail))]
    public async Task<IActionResult> CheckImagesWithDetail([FromForm] CheckItemWithImagesRequest req)
    {
        return Ok(await _ocrService.CheckBookInformationAsync(req.ToCheckedItemDto()));
    }
    [HttpPost(APIRoute.AIServices.OCRDetail,Name = nameof(OCRDetailAI))]
    public async Task<IActionResult> OCRDetailAI([FromForm] PredictRequest req,[FromRoute] int id)
    {
        return Ok(await _ocrService.OcrDetailAsync(req.ImageToPredict,id));
    }
    // [Authorize]
    // [HttpPost(APIRoute.AIServices.CheckBookEdition,Name = nameof(CheckBookEdition))]
    // public async Task<IActionResult> CheckBookEdition([FromForm] CheckBookEditionWithImageRequest dto)
    // {
    //     return Ok(await _ocrService.CheckBookInformationAsync(dto.ToCheckedBookEditionDto()));
    // }
    //
    // [Authorize]
    // [HttpPost(APIRoute.BookEdition.CheckImagesForTraining, Name = nameof(CheckImagesForTraining))]
    // public async Task<IActionResult> CheckImagesForTraining([FromForm] BaseTrainedModelRequest req,[FromRoute] int id)
    // {
    //     return Ok(await _ocrService.CheckTrainingInputAsync(id, req.ImageList));
    // }
}