using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
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
    [Authorize]
    [HttpPost(APIRoute.AIServices.CheckBookEdition,Name = nameof(CheckBookEdition))]
    public async Task<IActionResult> CheckBookEdition([FromForm] CheckBookEditionWithImageRequest dto)
    {
        return Ok(await _ocrService.CheckBookInformationAsync(dto.ToCheckedBookEditionDto()));
    }

    [Authorize]
    [HttpPost(APIRoute.AIServices.CheckImagesForTraining, Name = nameof(CheckImagesForTraining))]
    public async Task<IActionResult> CheckImagesForTraining([FromForm] CheckImagesForTrainingRequest req)
    {
        return Ok(await _ocrService.CheckTrainingInputAsync(req.BookEditionId, req.Images));
    }
}