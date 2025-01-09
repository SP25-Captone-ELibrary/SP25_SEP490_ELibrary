using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Books;
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
        // fix rule for EnglishName and VietnameseName with message in English and Vietnamese
        RuleFor(bc => bc.EnglishName)
            .NotEmpty()
            .WithMessage(isEng
                ? "EnglishName is required."
                : "Yêu cầu nhập tên tiếng Anh.")
            .NotNull()
            .WithMessage(isEng
                ? "EnglishName cannot be null."
                : "Tên tiếng Anh không được phép rỗng.")
            .Matches(@"^[A-Z][a-zA-Z]*$")
            .WithMessage(isEng
                ? "English name must not have space"
                : "Tên tiếng Anh không được có khoảng cách.");
        RuleFor(bc => bc.VietnameseName)
            .NotEmpty()
            .WithMessage(isEng
                ? "VietnameseName is required."
                : "Yêu cầu nhập tên tiếng Việt.")
            .NotNull()
            .WithMessage(isEng
                ? "VietnameseName cannot be null."
                : "Tên tiếng Việt không được phép rỗng.")
            .Matches(@"^[A-ZÀ-Ỵ][a-zà-ỵ]*(?: [A-Za-zÀ-Ỵà-ỵ]*)*$")
            .WithMessage(isEng
                ? "Vietnamese Name should not have special character. "
                : "Tên tiếng Việt không được chứa ký tự đặc biệt hoặc số,chữ đầu phải viết hoa.");
    }
}