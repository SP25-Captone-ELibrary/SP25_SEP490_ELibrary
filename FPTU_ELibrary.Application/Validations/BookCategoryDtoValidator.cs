using System.Text.RegularExpressions;
using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class BookCategoryDtoValidator : AbstractValidator<BookCategoryDto>
{
    public BookCategoryDtoValidator(string langContext)
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
            .Matches(@"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$")
            .WithMessage(isEng
                ? "English name should start with an uppercase letter for each word."
                : "Tên tiếng Anh phải bắt đầu bằng chữ in hoa cho mỗi từ.");
    }
}