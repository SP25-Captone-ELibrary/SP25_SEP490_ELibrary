using FluentValidation;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations;

public class ExcelValidator : AbstractValidator<IFormFile>
{
    public ExcelValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        RuleFor(x => x.ContentType).NotNull().Must(x => x.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                                                        || x.Equals("application/vnd.ms-excel")
                                                        || x.Equals("application/octet-stream")
                                                        || x.Equals("text/csv"))
            .WithMessage(isEng 
                ? "File type '.xlsx / .xlsm / .xlsb / .xlsx / .csv' are required"
                : "File yêu cầu '.xlsx / .xlsm / .xlsb / .xlsx / .csv'");
    }
}