using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class S3Service : IS3Service
{
    private readonly ISystemMessageService _msgService;
    private readonly ILogger _logger;
    private readonly AWSStorageSettings _monitor;
    private readonly AmazonS3Client _s3Client;

    public S3Service(IOptionsMonitor<AWSStorageSettings> monitor,
        ISystemMessageService msgService,ILogger logger)
    {
        _msgService = msgService;
        _logger = logger;
        _monitor = monitor.CurrentValue;
        _s3Client = new AmazonS3Client(
            _monitor.AccessKey,
            _monitor.SecretKey,
            RegionEndpoint.GetBySystemName(_monitor.Region));
    }
    public async Task<IServiceResult> GetFileAsync(AudioResourceType type, string fileName)
    {
        var request = new GetObjectRequest()
        {
            BucketName = _monitor.BucketName,
            Key = $"{(type == AudioResourceType.Original ? "original" : "watermarked")}/{fileName}",
        };

        var res = await _s3Client.GetObjectAsync(request);
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),res);
    }

    public Task<IServiceResult> GenerateMultipartUploadUrls(int totalParts)
    {
        throw new NotImplementedException();
    }

    public Task<IServiceResult> CompleteUploadMultipart(string s3PathKey, string uploadId, List<(int, string)> parts)
    {
        throw new NotImplementedException();
    }

    public async Task<IServiceResult> UploadFileAsync(AudioResourceType type, Stream audioFile, string fileName)
    {
        var request = new PutObjectRequest
        {
            BucketName = _monitor.BucketName,
            Key = $"{(type == AudioResourceType.Original ? "original" : "watermarked")}/{fileName}",
            InputStream = audioFile,
            ContentType = "audio/mpeg", //hardcode mp3 type
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(request);
        return new ServiceResult(ResultCodeConst.Cloud_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002), null);
    }

    public async Task<IServiceResult> GetFileUrlAsync(AudioResourceType type, string fileName)
    {
        var request = new GetPreSignedUrlRequest()
        {
            BucketName = _monitor.BucketName,
            Key = $"{(type == AudioResourceType.Original ? "original" : "watermarked")}/{fileName}",
            Expires = DateTime.Now.AddHours(1)
        };

        var url = await _s3Client.GetPreSignedURLAsync(request);
        return new ServiceResult(ResultCodeConst.SYS_Success0002
            ,await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), url);
    }
}