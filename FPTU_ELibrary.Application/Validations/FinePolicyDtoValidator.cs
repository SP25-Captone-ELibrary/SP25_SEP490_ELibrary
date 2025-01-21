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
                ? "Condition type is required"
                : "Yêu cầu nhập loại điều kiện");
        
        // validate the FineAmountPerDay property
        RuleFor(finePolicy => finePolicy.FineAmountPerDay)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.FineAmountPerDay < 1000)
                {
                    return isEng
                        ? "Fine amount per day must be at least 1.000 VND"
                        : "Số tiền phạt mỗi ngày phải ít nhất là 1.000 VND";
                }
                else if (e.FineAmountPerDay > 9999999999)
                {
                    return isEng
                        ? "Fine amount per day exceeds the maximum limit of 9.999.999.999 VND"
                        : "Số tiền phạt mỗi ngày vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
            
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid fine amount per day value"
                    : "Giá trị số tiền phạt mỗi ngày không hợp lệ";
            });

        // validate the FixedFineAmount property
        RuleFor(finePolicy => finePolicy.FixedFineAmount)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.FineAmountPerDay < 1000)
                {
                    return isEng
                        ? "Fixed fine amount must be at least 1.000 VND"
                        : "Số tiền phạt cố định phải ít nhất là 1.000 VND";
                }
                else if (e.FineAmountPerDay > 9999999999)
                {
                    return isEng
                        ? "Fixed fine amount exceeds the maximum limit of 9.999.999.999 VND"
                        : "Số tiền phạt cố định vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
            
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid Fixed fine amount value"
                    : "Giá trị số tiền phạt cố định không hợp lệ";
            });

        // validate the Description property
        RuleFor(finePolicy => finePolicy.Description)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Description must not exceed 255 characters"
                : "Mô tả không được vượt quá 255 ký tự");
    }
}