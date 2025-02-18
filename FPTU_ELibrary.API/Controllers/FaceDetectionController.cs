using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.FaceDetection;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class FaceDetectionController : ControllerBase
{
    private readonly IFaceDetectionService _faceDetectionSvc;

    public FaceDetectionController(IFaceDetectionService faceDetectionSvc)
    {
        _faceDetectionSvc = faceDetectionSvc;
    }
    
    [Authorize]
    [HttpPost(APIRoute.FaceDetection.Detect, Name = nameof(DetectFaceAsync))]
    public async Task<IActionResult> DetectFaceAsync([FromForm] PersonFaceDetectionRequest req)
    {
        return Ok(await _faceDetectionSvc.DetectFaceAsync(req.File, req.Attributes));
    }
}