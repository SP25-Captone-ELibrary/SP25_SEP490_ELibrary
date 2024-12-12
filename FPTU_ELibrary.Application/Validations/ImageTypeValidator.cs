using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations;

public class ImageTypeValidator : AbstractValidator<IFormFile>
{
    public ImageTypeValidator()
    {
        RuleFor(file => file.ContentType)
            .NotEmpty()
            .Must(BeAValidImageType)
            .WithMessage("Please upload a valid image file. Allowed types are: .jpg, .jpeg, .png, .gif, .svg, .bmp, .tiff, .ico");
    }

    private bool BeAValidImageType(string contentType)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/svg+xml", "image/bmp", "image/tiff", "image/x-icon" };
        return allowedTypes.Contains(contentType);
    }
}