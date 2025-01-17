using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemInstanceDtoValidator : AbstractValidator<LibraryItemInstanceDto>
{
    public LibraryItemInstanceDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        // TODO: Determine code specific type
        // Validate book edition copy barcode
        RuleFor(x => x.Barcode);
        
        // Add copy history validators
        RuleFor(x => x.LibraryItemConditionHistories)
            .ForEach(c =>
            {
                c.SetValidator(new LibraryItemConditionHistoryDtoValidator(langContext));
            });
    }
}