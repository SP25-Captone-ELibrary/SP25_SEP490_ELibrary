using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryCardPackageDtoValidator : AbstractValidator<LibraryCardPackageDto>
{
    public LibraryCardPackageDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Package name
        RuleFor(e => e.PackageName)
            .MaximumLength(100)
            .WithMessage(isEng
                ? "Package name cannot exceed 100 characters"
                : "Tên gói chỉ chứa tối đa 100 ký tự");
        // Price
        RuleFor(e => e.Price)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.Price < 1000)
                {
                    return isEng
                        ? "Price must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND";
                }
                else if (e.Price > 9999999999)
                {
                    return isEng
                        ? "Price exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
        
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid price value"
                    : "Giá trị tiền không hợp lệ";
            });
        // Package name
        RuleFor(e => e.Description)
            .MaximumLength(1000)
            .WithMessage(isEng
                ? "Description cannot exceed 1000 characters"
                : "Mô tả chỉ chứa tối đa 1000 ký tự");
    }
}