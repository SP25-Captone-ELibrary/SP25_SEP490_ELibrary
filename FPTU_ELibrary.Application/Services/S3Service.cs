using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AudioCloud;
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
        ISystemMessageService msgService, ILogger logger)
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
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), res);
    }

    public async Task<IServiceResult> GenerateMultipartUploadUrls(int totalParts)
    {
        var s3PathKey = Guid.NewGuid();
        var initiateRequest = new InitiateMultipartUploadRequest
            { BucketName = _monitor.BucketName, Key = $"original/{s3PathKey}", ContentType = "audio/mpeg" };
        var initiateResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);
        string uploadId = initiateResponse.UploadId;


        var urls = Enumerable.Range(1, totalParts)
            .Select(i =>
            {
                var presignedRequest = new GetPreSignedUrlRequest
                {
                    BucketName = _monitor.BucketName,
                    Key = $"original/{s3PathKey}",
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    UploadId = uploadId,
                    PartNumber = i
                };
                return _s3Client.GetPreSignedURL(presignedRequest);
            })
            .ToList();

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
            new GenerateMultipartUploadUrl
            {
                Urls = urls,
                UploadId = uploadId,
                S3PathKey = s3PathKey.ToString()
            });
    }

    public async Task<IServiceResult> CompleteUploadMultipart(string s3PathKey, string uploadId,
        List<(int, string)> parts)
    {
        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = _monitor.BucketName,
            Key = $"original/{s3PathKey}",
            UploadId = uploadId
        };

        foreach (var part in parts)
        {
            completeRequest.AddPartETags(new PartETag
            {
                PartNumber = part.Item1,
                ETag = part.Item2
            });
        }

        await _s3Client.CompleteMultipartUploadAsync(completeRequest);
        return new ServiceResult(ResultCodeConst.Cloud_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002), null);
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

    public async Task<IServiceResult> UploadFileAsync(AudioResourceType type, Stream audioFile)
    {
        var fileName = Guid.NewGuid();
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
            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002), fileName);
    }

    public async Task<IServiceResult> DeleteFileAsync(AudioResourceType type ,string fileName)
    {
        var request = new DeleteObjectRequest()
        {
            BucketName = _monitor.BucketName,
            Key = $"{(type == AudioResourceType.Original ? "original" : "watermarked")}/{fileName}"
        };
        await _s3Client.DeleteObjectAsync(request);
        return new ServiceResult(ResultCodeConst.SYS_Success0004,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
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
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), url);
    }
}