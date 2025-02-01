using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class WarehouseTrackingDetailDtoValidator : AbstractValidator<WarehouseTrackingDetailDto>
{
    public WarehouseTrackingDetailDtoValidator(string langContext)
    {
        var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = lang == SystemLanguage.English;
        
        // Item name
        RuleFor(e => e.ItemName)
            .NotEmpty()
            .WithMessage(isEng
                ? "Item name is required" 
                : "Tên tài liệu không được rỗng")
            .Length(5, 255)
            .WithMessage(isEng
                ? "Item name must be between 5 and 225 characters"
                : "Tên tài liệu phải nằm trong khoảng 5 đến 255 ký tự");
        // Isbn
        RuleFor(e => e.Isbn)
            .Must(str => str == null || (ISBN.IsValid(str, out _) && ISBN.CleanIsbn(str).Length <= 13))
            .WithMessage(isEng
                ? "ISBN is not valid"
                : "Mã ISBN không hợp lệ");
        // Unit price
        RuleFor(e => e.UnitPrice)
            .InclusiveBetween(0, 9999999999)
            .WithMessage(e => 
            {
                if (e.UnitPrice < 0)
                {
                    return isEng
                        ? "Unit price must greater than or equals to 0"
                        : "Giá phải lớn hơn hoặc bằng 0";
                }
                else if (e.UnitPrice > 9999999999)
                {
                    return isEng
                        ? "Unit price exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
        
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid unit price value"
                    : "Giá trị không hợp lệ";
            });
        // Total amount
        RuleFor(e => e.TotalAmount)
            .InclusiveBetween(0, 9999999999)
            .WithMessage(e => 
            {
                if (e.TotalAmount < 0)
                {
                    return isEng
                        ? "Total amount must greater than or equals to 0"
                        : "Giá phải lớn hơn hoặc bằng 0";
                }
                else if (e.TotalAmount > 9999999999)
                {
                    return isEng
                        ? "Total amount exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
        
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid total amount value"
                    : "Giá trị không hợp lệ";
            });
        // Total item
        RuleFor(e => e.ItemTotal)
            .Must(v => v < int.MaxValue)
            .WithMessage(isEng ? "Total item is not valid" : "Tổng số lượng không hợp lệ hoặc quá lớn");
    }
}