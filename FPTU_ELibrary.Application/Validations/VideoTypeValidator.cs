using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations;

public class VideoTypeValidator : AbstractValidator<IFormFile>
{
    public VideoTypeValidator()
    {
        RuleFor(file => file.ContentType)
            .NotEmpty()
            .Must(BeAValidMediaType)
            .WithMessage("Please upload a valid media file. Allowed types are: .mp4, .mov, .avi, .wmv, .mkv, .mp3, .wav, .aac, .flac");
    }

    private bool BeAValidMediaType(string contentType)
    {
        var allowedTypes = new[]
        {
            "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-ms-wmv", "video/x-matroska",
            "audio/mpeg", "audio/wav", "audio/aac", "audio/flac"
        };
        return allowedTypes.Contains(contentType);
    }
}