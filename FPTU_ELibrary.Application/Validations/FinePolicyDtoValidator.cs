using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class FinePolicyDtoValidator : AbstractValidator<FinePolicyDto>
{
    public FinePolicyDtoValidator(string langContext)
    {
        // get the language enum from the langContext
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        // check if the language is English
        var isEng = langEnum == SystemLanguage.English;

        // validate the ConditionType property
        RuleFor(finePolicy => finePolicy.ConditionType)
            .NotNull()
            .WithMessage(isEng
                ? "ConditionType is required"
                : "Yêu cầu nhập loại điều kiện")
            .NotEmpty()
            .WithMessage(isEng
                ? "ConditionType cannot be empty"
                : "Loại điều kiện không được phép rỗng");
        
        // validate the FineAmountPerDay property
        RuleFor(finePolicy => finePolicy.FineAmountPerDay)
            .GreaterThan(0)
            .LessThan(1000000000)
            .WithMessage(isEng
                ? "FineAmountPerDay must be greater than 0 and less than 1000000000"
                : "Số tiền phạt mỗi ngày phải lớn hơn 0");

        // validate the FixedFineAmount property
        RuleFor(finePolicy => finePolicy.FixedFineAmount)
            .GreaterThan(0)
            .LessThan(1000000000)
            .WithMessage(isEng
                ? "FixedFineAmount must be greater than 0 less than 1000000000"
                : "Số tiền phạt cố định phải lớn hơn 0");

        // validate the Description property
        RuleFor(finePolicy => finePolicy.Description)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Description must not exceed 255 characters"
                : "Mô tả không được vượt quá 255 ký tự");
    }
}