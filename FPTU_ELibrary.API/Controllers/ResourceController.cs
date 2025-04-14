using System.Security.Claims;
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
    private readonly IS3Service _s3Service;

    public ResourceController(ICloudinaryService cloudService,
        IS3Service s3Service)
    {
        _cloudService = cloudService;
        _s3Service = s3Service;
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
    [HttpPost(APIRoute.Resource.PublicUploadImage, Name = nameof(PublicUploadImageAsync))]
    public async Task<IActionResult> PublicUploadImageAsync([FromForm] UploadImageRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cloudService.PublicUploadAsync(
            email: email ?? string.Empty,
            file: req.File,
            fileType: FileType.Image,
            resourceType: (ResourceType) Enum.Parse(typeof(ResourceType), req.ResourceType)));
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

    [Authorize]
    [HttpPost(APIRoute.Resource.UploadLargeVideo, Name = nameof(UploadLargeVideoAsync))]
    public async Task<IActionResult> UploadLargeVideoAsync([FromBody] UploadLargeVideoRequest req)
    {
        return Ok(await _cloudService.UploadLargeVideo(req.ProviderIds));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Resource.GetPartUrls, Name = nameof(GetPartUrls))]
    public async Task<IActionResult> GetPartUrls([FromQuery] int totalParts)
    {
        return Ok(await _s3Service.GenerateMultipartUploadUrls(totalParts));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Resource.CompleteUploadMultiPart, Name = nameof(CompleteUploadMultiPart))]
    public async Task<IActionResult> CompleteUploadMultiPart([FromBody] CompleteUploadMultiPartRequest req)
    {
        return Ok(await _s3Service.CompleteUploadMultipart(req.S3PathKey,req.UploadId,
            req.ConvertToTuple()));
    }
    [Authorize]
    [HttpPost(APIRoute.Resource.UploadSmallAudio, Name = nameof(UploadSmallAudioAsync))]
    public async Task<IActionResult> UploadSmallAudioAsync([FromForm] UploadSmallAudioRequest req)
    {
        return Ok(   await _s3Service.UploadFileAsync(AudioResourceType.Original, req.File.OpenReadStream()));
    }
    
}