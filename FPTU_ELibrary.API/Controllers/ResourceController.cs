using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Resource;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    private readonly ICloudinaryService _cloudService;

    public ResourceController(ICloudinaryService cloudService)
    {
        _cloudService = cloudService;
    }

    [Authorize]
    [HttpGet(APIRoute.Resource.GetAllType, Name = nameof(GetAllResourceType))]
    public async Task<IActionResult> GetAllResourceType()
    {
        return await Task.FromResult(
            Ok(new List<string> {
                nameof(ResourceType.Profile),
                nameof(ResourceType.BookImage),
                nameof(ResourceType.BookAudio)
            })
        );
    }
    
    [Authorize]
    [HttpPost(APIRoute.Resource.UploadImage, Name = nameof(UploadImageAsync))]
    public async Task<IActionResult> UploadImageAsync([FromForm] UploadImageRequest req)
    {
        return Ok(await _cloudService.UploadAsync(req.File, FileType.Image,
            (ResourceType) Enum.Parse(typeof(ResourceType), req.ResourceType)));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Resource.UploadVideo, Name = nameof(UploadVideoAsync))]
    public async Task<IActionResult> UploadVideoAsync([FromForm] UploadImageRequest req)
    {
        return Ok(await _cloudService.UploadAsync(req.File, FileType.Video,
            (ResourceType) Enum.Parse(typeof(ResourceType), req.ResourceType)));
    }

    [Authorize]
    [HttpPut(APIRoute.Resource.UpdateImage, Name = nameof(UpdateImageAsync))]
    public async Task<IActionResult> UpdateImageAsync([FromForm] UpdateResourceRequest req)
    {
        return Ok(await _cloudService.UpdateAsync(req.PublicId, req.File, FileType.Image));
    }
    
    [Authorize]
    [HttpPut(APIRoute.Resource.UpdateVideo, Name = nameof(UpdateVideoAsync))]
    public async Task<IActionResult> UpdateVideoAsync([FromForm] UpdateResourceRequest req)
    {
        return Ok(await _cloudService.UpdateAsync(req.PublicId, req.File, FileType.Video));
    }

    [Authorize]
    [HttpDelete(APIRoute.Resource.DeleteImage, Name = nameof(DeleteImageAsync))]
    public async Task<IActionResult> DeleteImageAsync([FromQuery] string publicId)
    {
        return Ok(await _cloudService.DeleteAsync(publicId, FileType.Image));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Resource.DeleteVideo, Name = nameof(DeleteVideoAsync))]
    public async Task<IActionResult> DeleteVideoAsync([FromQuery] string publicId)
    {
        return Ok(await _cloudService.DeleteAsync(publicId, FileType.Video));
    }
}