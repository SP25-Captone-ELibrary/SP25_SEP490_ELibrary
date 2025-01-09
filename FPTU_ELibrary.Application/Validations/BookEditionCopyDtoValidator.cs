using FluentValidation;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class BookEditionCopyDtoValidator : AbstractValidator<BookEditionCopyDto>
{
    public BookEditionCopyDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        // TODO: Determine code specific type
        // Validate book edition copy code
        RuleFor(x => x.Code);
        
        // Validate book edition copy barcode
        RuleFor(x => x.Barcode);
        
        // Add copy history validators
        RuleFor(x => x.CopyConditionHistories)
            .ForEach(c =>
            {
                c.SetValidator(new CopyConditionHistoryDtoValidator(langContext));
            });
    }
}