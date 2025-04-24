using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryClosureDayDtoValidator : AbstractValidator<LibraryClosureDayDto>
{
    public LibraryClosureDayDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        // Vie Description
        RuleFor(e => e.VieDescription)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Vietnamese description must not exceed 255 characters"
                : "Mô tả tiếng Việt không vượt quá 255 ký tự");
        
        // Eng Description
        RuleFor(e => e.EngDescription)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "English description must not exceed 255 characters"
                : "Mô tả tiếng Anh không vượt quá 255 ký tự");
    }
}