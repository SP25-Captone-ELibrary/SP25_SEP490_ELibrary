using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemConditionDtoValidator : AbstractValidator<LibraryItemConditionDto>
{
    public LibraryItemConditionDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        RuleFor(e => e.EnglishName)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "English name must not exceed 50 characters"
                : "Tên tiếng anh không vượt quá 50 ký tự");
        
        RuleFor(e => e.VietnameseName)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Vietnamese name must not exceed 50 characters"
                : "Tên tiếng việt không vượt quá 50 ký tự");
    }
}