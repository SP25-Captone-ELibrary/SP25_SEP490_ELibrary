using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class CategoryDtoValidator : AbstractValidator<CategoryDto>
{
    public CategoryDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Prefix 
        RuleFor(c => c.Prefix)
            .NotEmpty()
            .WithMessage(isEng
                ? "Please add prefix pattern for item classification number such as SGK, SD, TK"
                : "Vui lòng nhập mẫu tiền tố cho đăng ký cá biệt. Ví dụ: SGK, SD, TK")
            .Matches(@"^[A-Za-z]{1,3}$")
            .WithMessage(isEng
                ? "The prefix must contain only letters (A-Z)"
                : "Tiền tố chỉ được chứa các chữ cái (A-Z)")
            .MaximumLength(3)
            .WithMessage(isEng
                ? "The prefix cannot be longer than 3 characters"
                : "Tiền tố không được dài quá 3 ký tự");
        // English name
        RuleFor(c => c.EnglishName)
            .NotEmpty()
            .WithMessage(isEng
                ? "EnglishName is required"
                : "Yêu cầu nhập tên tiếng Anh")
            .NotNull()
            .WithMessage(isEng
                ? "EnglishName cannot be null"
                : "Tên tiếng Anh không được phép rỗng")
            .Matches(@"^([A-Z][a-zA-Z]*)(\s[a-zA-Z]+)*$")
            .WithMessage(isEng
                ? "English name should start with an uppercase letter for the first word and not include numbers"
                : "Tên tiếng Anh bắt đầu bằng chữ cái viết hoa cho từ đầu tiên và không chứa số");
        // Vietnamese name
        RuleFor(c => c.VietnameseName)
            .NotEmpty()
            .WithMessage(isEng
                ? "VietnameseName is required"
                : "Yêu cầu nhập tên tiếng Việt")
            .NotNull()
            .WithMessage(isEng
                ? "VietnameseName cannot be null"
                : "Tên tiếng Việt không được phép rỗng")
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[a-zA-ZÀ-Ỵa-zà-ỵ]+)*$")
            .WithMessage(isEng
                ? "Vietnamese Name should start with an uppercase letter for the first word and not include numbers"
                : "Tên tiếng Việt bắt đầu bằng chữ cái viết hoa cho từ đầu tiên và không chứa số");
    }
}