using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    public TransactionDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        RuleFor(t => t.Description)
            .Length(1, 100)
            .WithMessage(isEng 
                ? "Description must be between 1 and 100 characters long" 
                : "Mô tả phải có độ dài từ 1 đến 100 ký tự");
        RuleFor(t => t.Amount)
            .GreaterThan(0)
            .WithMessage(isEng 
                ? "Amount must be greater than 0" 
                : "Số tiền phải lớn hơn 0");

        RuleFor(t => t)
            .Must(t =>
            {
                int count = (t.FineId.HasValue ? 1 : 0) +
                            (t.ResourceId.HasValue ? 1 : 0) +
                            (t.LibraryCardPackageId.HasValue ? 1 : 0);
                return count == 1;
            })
            .WithMessage(isEng 
                ? "Choose at least 1 product type" 
                : "Vui lòng chọn 1 loại sản phẩm");
    }
}