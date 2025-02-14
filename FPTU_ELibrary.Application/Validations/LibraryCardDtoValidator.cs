using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryCardDtoValidator : AbstractValidator<LibraryCardDto>
{
    public LibraryCardDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Fullname
        RuleFor(e => e.FullName)
            .NotEmpty()
            .WithMessage(isEng ? "Full Name is required" : "Họ và tên không được rỗng")
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
            .WithMessage(isEng 
                ? "Fullname should start with an uppercase letter for each word, not include number or special character" 
                : "Họ và tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ, không chứa số hoặc ký tự đặc biệt")
            .MaximumLength(200)
            .WithMessage(isEng
                ? "Full name cannot exceed 200 characters"
                : "Họ và tên không vượt quá 200 ký tự");
        // Avatar   
        RuleFor(e => e.Avatar)
            .Must(str => !string.IsNullOrEmpty(str) && StringUtils.IsValidUrl(str))
            .WithMessage(isEng
                ? "Invalid avatar image"
                : "Hình ảnh không hợp lệ");
    }
}