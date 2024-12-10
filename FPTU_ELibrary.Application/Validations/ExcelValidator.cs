using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations;

public class ExcelValidator : AbstractValidator<IFormFile>
{
    public ExcelValidator()
    {
        RuleFor(x => x.ContentType).NotNull().Must(x => x.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                                                        || x.Equals("application/vnd.ms-excel")
                                                        || x.Equals("application/octet-stream")
                                                        || x.Equals("text/csv"))
            .WithMessage("File type '.xlsx / .xlsm / .xlsb / .xlsx / .csv' are required");
    }
}