using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class BorrowRequestDtoValidator : AbstractValidator<BorrowRequestDto>
{
    public BorrowRequestDtoValidator(string langContext)
    {
        var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = lang == SystemLanguage.English;
        
        // Description
        RuleFor(e => e.Description)
            .MaximumLength(250)
            .WithMessage(isEng
                ? "Description must not exceed 250 characters"
                : "Mô tả không vượt quá 250 ký tự");
    }
}