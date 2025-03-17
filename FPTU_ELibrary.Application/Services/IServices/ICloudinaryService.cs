using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface ICloudinaryService
{
    Task<IServiceResult> UploadAsync(IFormFile file, FileType fileType, ResourceType resourceType);
    Task<IServiceResult> UpdateAsync(string publicId, IFormFile file, FileType fileType);
    Task<IServiceResult> DeleteAsync(string publicId, FileType fileType);
    Task<IServiceResult> IsExistAsync(string publicId, FileType fileType);
    Task<IServiceResult> GetMediaUrlAsync(string publicId, FileType fileType);
    Task<IServiceResult> BuildMediaUrlAsync(string publicId, FileType fileType);
    Task<IServiceResult> UploadLargeVideo(List<string> providerIds);
    // Task<IServiceResult> GetRangeMediaUrlAsync(List<string> publicIds, FileType fileType);
}