using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class FineDtoValidator : AbstractValidator<FineDto>
{
    public FineDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Fine amount 
        RuleFor(e => e.FineAmount)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.FineAmount < 1000)
                {
                    return isEng
                        ? "Fine amount per day must be at least 1.000 VND"
                        : "Số tiền phạt mỗi ngày phải ít nhất là 1.000 VND";
                }
                else if (e.FineAmount > 9999999999)
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
        
        // Fine note
        RuleFor(e => e.FineNote)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Fine not must not exceed 255 characters"
                : "Mô tả không được vượt quá 255 ký tự");
    }   
}