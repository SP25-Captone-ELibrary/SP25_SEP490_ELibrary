using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Cloudinary;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using ResourceType = FPTU_ELibrary.Domain.Common.Enums.ResourceType;
using CloudinaryResourceType = CloudinaryDotNet.Actions.ResourceType;

namespace FPTU_ELibrary.Application.Services;

public class CloudinaryService : ICloudinaryService
{
    // Lazy services
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    
    private readonly Cloudinary _cloudinary;
    private readonly ILogger _logger;
    private readonly ISystemMessageService _msgService;
    private readonly CloudinarySettings _cloudSettings;

    public CloudinaryService(
        // Lazy services
        Lazy<IUserService<UserDto>> userSvc,   
        
        // Normal services
        IOptionsMonitor<CloudinarySettings> monitor,
        ISystemMessageService msgService,
        Cloudinary cloudinary,
        ILogger logger)
    {
        _userSvc = userSvc;
        _cloudSettings = monitor.CurrentValue;
        _cloudinary = cloudinary;
        _msgService = msgService;
        _logger = logger;
    }

    public async Task<IServiceResult> PublicUploadAsync(string email, IFormFile file, FileType fileType, ResourceType resourceType)
    {
        try
        {
            // Check exist user by email
            var isExistUser = (await _userSvc.Value.AnyAsync(u => Equals(u.Email, email))).Data is true;
            if (!isExistUser) throw new ForbiddenException("Not allow to access"); // Forbid to access

            // Get cloudinary directory by resource type
            var directory = GetDirectoryFromResourceType(resourceType);
            // Custom public id, ends with random digits
            // var uniqueIdWithTimestamp = $"{directory}/{Guid.NewGuid().ToString()}";
            var uniqueIdWithTimestamp = Guid.NewGuid().ToString();

            // Retrieve current language
            var currentLanguage = LanguageContext.CurrentLanguage;

            switch (fileType)
            {
                // IMAGE
                case FileType.Image:
                    // Validate image 
                    var imageValidationRes = await new ImageTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!imageValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid image file type",
                            imageValidationRes.ToProblemDetails().Errors);
                    }

                    // Initializes image upload params
                    var imageUploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(uniqueIdWithTimestamp, file.OpenReadStream()),
                        PublicId = uniqueIdWithTimestamp
                    };
                    // Upload image file to cloudinary
                    var imageResult = await _cloudinary.UploadAsync(imageUploadParams);

                    // Success 
                    if (imageResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0001),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = imageResult.SecureUrl.ToString(),
                                PublicId = imageResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0001));
                // VIDEO
                case FileType.Video:
                    // Validate video 
                    var videoValidationRes = await new VideoTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!videoValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid video file type",
                            videoValidationRes.ToProblemDetails().Errors);
                    }

                    // Initializes image upload params
                    var videoUploadParams = new VideoUploadParams()
                    {
                        File = new FileDescription(uniqueIdWithTimestamp, file.OpenReadStream()),
                        PublicId = uniqueIdWithTimestamp
                    };
                    // Upload image file to cloudinary
                    var videoResult = await _cloudinary.UploadAsync(videoUploadParams);

                    // Success 
                    if (videoResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = videoResult.SecureUrl.ToString(),
                                PublicId = videoResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0002));
                default:
                    return new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process upload image from public");
        }
    }
    
    public async Task<IServiceResult> UploadAsync(IFormFile file, FileType fileType, ResourceType resourceType)
    {
        // Get cloudinary directory by resource type
        var directory = GetDirectoryFromResourceType(resourceType);
        // Custom public id, ends with random digits
        // var uniqueIdWithTimestamp = $"{directory}/{Guid.NewGuid().ToString()}";
        var uniqueIdWithTimestamp = Guid.NewGuid().ToString();

        // Retrieve current language
        var currentLanguage = LanguageContext.CurrentLanguage;

        try
        {
            switch (fileType)
            {
                // IMAGE
                case FileType.Image:
                    // Validate image 
                    var imageValidationRes = await new ImageTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!imageValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid image file type",
                            imageValidationRes.ToProblemDetails().Errors);
                    }

                    // Initializes image upload params
                    var imageUploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(uniqueIdWithTimestamp, file.OpenReadStream()),
                        PublicId = uniqueIdWithTimestamp
                    };
                    // Upload image file to cloudinary
                    var imageResult = await _cloudinary.UploadAsync(imageUploadParams);

                    // Success 
                    if (imageResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0001),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = imageResult.SecureUrl.ToString(),
                                PublicId = imageResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0001));
                // VIDEO
                case FileType.Video:
                    // Validate video 
                    var videoValidationRes = await new VideoTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!videoValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid video file type",
                            videoValidationRes.ToProblemDetails().Errors);
                    }

                    // Initializes image upload params
                    var videoUploadParams = new VideoUploadParams()
                    {
                        File = new FileDescription(uniqueIdWithTimestamp, file.OpenReadStream()),
                        PublicId = uniqueIdWithTimestamp
                    };
                    // Upload image file to cloudinary
                    var videoResult = await _cloudinary.UploadAsync(videoUploadParams);

                    // Success 
                    if (videoResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = videoResult.SecureUrl.ToString(),
                                PublicId = videoResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0002));
                default:
                    return new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process upload resource to cloudinary");
        }
    }

    public async Task<IServiceResult> UpdateAsync(string publicId, IFormFile file, FileType fileType)
    {
        try
        {
            // Retrieve current language
            var currentLanguage = LanguageContext.CurrentLanguage;

            var checkExistResult = await IsExistAsync(publicId, fileType);
            if (checkExistResult.Data is false) return checkExistResult;

            switch (fileType)
            {
                // IMAGE
                case FileType.Image:
                    // Validate image 
                    var imageValidationRes = await new ImageTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!imageValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid image file type",
                            imageValidationRes.ToProblemDetails().Errors);
                    }

                    // Image upload params
                    var imageUploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(publicId, file.OpenReadStream()),
                        PublicId = publicId,
                        // Invalidate = true,
                        Overwrite = true
                    };

                    var imageResult = await _cloudinary.UploadAsync(imageUploadParams);

                    // Success
                    if (imageResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0001),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = imageResult.SecureUrl.ToString(),
                                PublicId = imageResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0001));
                // VIDEO
                case FileType.Video:
                    // Validate video 
                    var videoValidationRes = await new VideoTypeValidator(currentLanguage).ValidateAsync(file);
                    if (!videoValidationRes.IsValid)
                    {
                        throw new UnprocessableEntityException("Invalid video file type",
                            videoValidationRes.ToProblemDetails().Errors);
                    }

                    // Video upload params
                    var videoUploadParams = new VideoUploadParams()
                    {
                        File = new FileDescription(publicId, file.OpenReadStream()),
                        PublicId = publicId,
                        // Invalidate = true,
                        Overwrite = true
                    };
                    var videoResult = await _cloudinary.UploadAsync(videoUploadParams);

                    // Success
                    if (videoResult.StatusCode == HttpStatusCode.OK)
                    {
                        return new ServiceResult(ResultCodeConst.Cloud_Success0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0002),
                            new CloudinaryResultDto()
                            {
                                SecureUrl = videoResult.SecureUrl.ToString(),
                                PublicId = videoResult.PublicId,
                            });
                    }

                    // Error
                    return new ServiceResult(ResultCodeConst.Cloud_Fail0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0002));
                default:
                    return new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update resource to cloudinary");
        }
    }

    public async Task<IServiceResult> DeleteAsync(string publicId, FileType fileType)
    {
        try
        {
            // Check exist cloud resource by public id
            var existResource = await _cloudinary.GetResourceAsync(publicId);
            if (existResource == null || existResource.Error != null) // Not found
            {
                return fileType switch
                {
                    // Not found image code
                    FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Warning0001,
                        // Not found video code
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001)),
                    FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Warning0002,
                        // File type is not valid
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0002)),
                    _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001))
                };
            }

            var deleteParams = new DeletionParams(publicId)
            {
                Invalidate = true,
                ResourceType = fileType.Equals(FileType.Image)
                    ? CloudinaryResourceType.Image
                    : CloudinaryResourceType.Video
            };

            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

            // Not found
            if (deleteResult.Result.Contains("not found"))
            {
                return fileType switch
                {
                    // Not found image code
                    FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Warning0001,
                        // Not found video code
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001)),
                    FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Warning0002,
                        // File type is not valid
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0002)),
                    _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001))
                };
            }

            // Success
            if (deleteResult.StatusCode == HttpStatusCode.OK)
            {
                return fileType switch
                {
                    // Not found image code
                    FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Success0003,
                        // Not found video code
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0003)),
                    FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Success0004,
                        // File type is not valid
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Success0004)),
                    _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001))
                };
            }

            // Error 
            return fileType switch
            {
                // Not found image code
                FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Fail0003,
                    // Not found video code
                    await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0003)),
                FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Fail0004,
                    // File type is not valid
                    await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Fail0004)),
                _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001))
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete resource to cloudinary");
        }
    }

    public async Task<IServiceResult> IsExistAsync(string publicId, FileType fileType)
    {
        try
        {
            // Check exist cloud resource by public id
            var existResource = fileType == FileType.Video
                ? await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                    { ResourceType = CloudinaryResourceType.Video })
                : await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                    { ResourceType = CloudinaryResourceType.Image });
            if (existResource == null || existResource.Error != null) // Not found
            {
                return fileType switch
                {
                    // Not found image code
                    FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Warning0001,
                        // Not found video code
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001), false),
                    FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Warning0002,
                        // File type is not valid
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0002), false),
                    _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001), false)
                };
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check exist cloud resource");
        }
    }

    public async Task<IServiceResult> GetMediaUrlAsync(string publicId, FileType fileType)
    {
        try
        {
            // Check exist cloud resource by public id
            var existResource = fileType == FileType.Video
                ? await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                    { ResourceType = CloudinaryResourceType.Video })
                : await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                    { ResourceType = CloudinaryResourceType.Image });
            if (existResource == null || existResource.Error != null) // Not found
            {
                return fileType switch
                {
                    // Not found image code
                    FileType.Image => new ServiceResult(ResultCodeConst.Cloud_Warning0001,
                        // Not found video code
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001)),
                    FileType.Video => new ServiceResult(ResultCodeConst.Cloud_Warning0002,
                        // File type is not valid
                        await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0002)),
                    _ => new ServiceResult(ResultCodeConst.File_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001))
                };
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                existResource.SecureUrl ?? existResource.Url);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get cloud resource");
        }
    }

    public async Task<IServiceResult> BuildMediaUrlAsync(string publicId, FileType fileType)
    {
        try
        {
            // If you only need to generate the URL and don’t need to check if the resource exists:
            string mediaUrl = fileType == FileType.Video
                ? _cloudinary.Api.UrlVideoUp.BuildUrl(publicId)
                : _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
        
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), mediaUrl);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating Cloudinary URL.");
            throw new Exception("Error occurred while generating Cloudinary URL", ex);
        }
    }

    private string? GetDirectoryFromResourceType(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Profile => _cloudSettings.ProfileDirectory,
                ResourceType.BookAudio => _cloudSettings.BookAudioDirectory,
                ResourceType.BookImage => _cloudSettings.BookImageDirectory,
                _ => null
            };
        }
    }