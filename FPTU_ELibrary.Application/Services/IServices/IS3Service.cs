using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IS3Service
{
    Task<IServiceResult> GetFileAsync(AudioResourceType type, string fileName);
    Task<IServiceResult> GenerateMultipartUploadUrls(int totalParts);
    Task<IServiceResult> CompleteUploadMultipart(string s3PathKey, string uploadId, List<(int,string)> parts);
    Task<IServiceResult> UploadFileAsync(AudioResourceType type, Stream audioFile, string fileName);
    Task<IServiceResult> GetFileUrlAsync(AudioResourceType type, string fileName);
}