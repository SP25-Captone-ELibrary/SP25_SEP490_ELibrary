using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemConditionHistoryDtoValidator : AbstractValidator<LibraryItemConditionHistoryDto>
{
    public LibraryItemConditionHistoryDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        RuleFor(e => e.Condition)
            .Must(str => !string.IsNullOrEmpty(str) 
                         && Enum.TryParse(typeof(LibraryItemConditionStatus), str, true, out _))
            .WithMessage(isEng
                ? "Condition status is not valid"
                : "Tình trạng bản in không hợp lệ");
    }
}