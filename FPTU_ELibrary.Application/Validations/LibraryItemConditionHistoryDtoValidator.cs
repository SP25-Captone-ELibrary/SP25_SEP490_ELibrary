using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemConditionHistoryDtoValidator : AbstractValidator<LibraryItemConditionHistoryDto>
{
    public LibraryItemConditionHistoryDtoValidator(string langContext)
    {
        RuleFor(e => e.Condition)
            .SetValidator(new LibraryItemConditionDtoValidator(langContext));
    }
}