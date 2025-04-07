using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class WarehouseTrackingDtoValidator : AbstractValidator<WarehouseTrackingDto>
{
    public WarehouseTrackingDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;    
        
        // Receipt number
        RuleFor(e => e.ReceiptNumber)
            // .NotNull()
            // .WithMessage(isEng
            //     ? "Receipt number is required"
            //     : "Số biên lai không được phép rỗng")
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Reciept number cannot exceed 50 characters"
                : "Số biên lai không được quá 50 ký tự");
        // Total item
        RuleFor(e => e.TotalItem)
            .Must(i => i > 0)
            .WithMessage(isEng
                ? "Required at least 1 item to create warehouse tracking"
                : "Cần ít nhất 1 tài liệu để tạo theo dõi kho")
            .Must(i => i < int.MaxValue)
            .WithMessage(isEng
                ? "Total item is not valid"
                : "Tổng số tài liệu không hợp lệ");
        // Total amount
        RuleFor(e => e.TotalAmount)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.TotalAmount < 1000)
                {
                    return isEng
                        ? "Total amount must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND";
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
                    : "Giá trị tổng chi phí không hợp lệ";
            });
        // Transfer location
        RuleFor(e => e.TransferLocation)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Transfer location cannot exceed 255 characters"
                : "Nơi trao đổi không vượt quá 255 ký tự");
        // Description
        RuleFor(e => e.Description)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Description cannot exceed 255 characters"
                : "Mô tả không vượt quá 255 ký tự");
        // Entry date
        RuleFor(e => e.EntryDate)
            .NotNull()
            .WithMessage(isEng
                ? "Entry date is required"
                : "Ngày thực hiện không được phép rỗng");
        // Data finalization date
        RuleFor(e => e.DataFinalizationDate)
            .Must((w, d) => !d.HasValue  || d.Value.Date >= w.EntryDate.Date)
            .WithMessage(isEng
                ? "Data finalization date must exceed than entry date"
                : "Ngày chốt số liệu không được nhỏ hơn ngày lập biên bản");
        // Expected return date
        RuleFor(e => e.ExpectedReturnDate)
            .Must((w, d) => !d.HasValue  || d.Value.Date > w.EntryDate.Date)
            .WithMessage(isEng
                ? "Expected return date must exceed than entry date"
                : "Ngày trả dự kiến phải lớn hơn ngày thực hiện");
        // Warehouse tracking details
        RuleFor(e => e.WarehouseTrackingDetails)
            // Iterate each tracking details
            .ForEach(wtd =>
            {   
                // Process validator if exist any 
                if (wtd != null) wtd.SetValidator(new WarehouseTrackingDetailDtoValidator(langContext));
            });
    }   
}