using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryResourceDtoValidator : AbstractValidator<LibraryResourceDto>
{
    public LibraryResourceDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // File format
        RuleFor(e => e.FileFormat)
            .Must(str => Enum.TryParse(typeof(FileType), str, out _))
            .WithMessage(isEng
                ? "File format is invalid"
                : "Loại tệp không hợp lệ");
        
        // PublicId
        // RuleFor(e => e.ProviderPublicId)
        //     .Must(str => Guid.TryParse(str, out _))
        //     .WithMessage(isEng
        //         ? "Invalid provider id"
        //         : "Provider id không hợp lệ");
        
        // Resource type
        RuleFor(e => e.ResourceType)
            .Must(str => !string.IsNullOrEmpty(str) && Enum.TryParse(typeof(LibraryResourceType), str, out _))
            .WithMessage(isEng
                ? "Resource type is invalid"
                : "Loại tài nguyên sách không hợp lệ");
        
        // // Resource Url
        // RuleFor(e => e.ResourceUrl)
        //     .Must(str => 
        //         !string.IsNullOrEmpty(str) && StringUtils.IsValidUrl(str))
        //     .WithMessage(isEng
        //         ? "Resource URL is invalid"
        //         : "Đường dẫn tài nguyên sách không hợp lệ");
        
        // Resource size
        RuleFor(e => e.ResourceSize)
            .Must(e => e != null && e > 0 &&
                       StringUtils.IsDecimal(e.ToString() ?? string.Empty))
            .WithMessage(isEng
                ? "Resource size is invalid"
                : "Kích thước tài nguyên sách không hợp lệ");
        
        // Provider
        RuleFor(e => e.Provider)
            .Must(str => Enum.TryParse(typeof(ResourceProvider), str, out _))
            .WithMessage(isEng
                ? "Resource provider is invalid"
                : "Bên cung cấp tài nguyên không hợp lệ");
    }
}