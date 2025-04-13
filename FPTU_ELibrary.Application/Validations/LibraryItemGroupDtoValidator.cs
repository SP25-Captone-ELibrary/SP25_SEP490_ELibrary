using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemGroupDtoValidator : AbstractValidator<LibraryItemGroupDto>
{ 
    public LibraryItemGroupDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Title 
        RuleFor(e => e.Title)
            .NotNull()
            .WithMessage(isEng
                ? "Title is required" 
                : "Yêu cầu nhập tiêu đề cho tài liệu")
            .NotEmpty()
            .WithMessage(isEng
                ? "Title is not empty" 
                : "Tiêu đề tài liệu không được rỗng")
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Title must not exceed than 255 characters"
                : "Tiêu đề của tài liệu phải nhỏ hơn 255 ký tự");
        // Subtitle 
        RuleFor(e => e.SubTitle)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Sub title must not exceed than 255 characters"
                : "Tiêu đề bổ sung của tài liệu phải nhỏ hơn 255 ký tự");
        // DDC number
        RuleFor(e => e.ClassificationNumber)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "DDC number must not exceed 50 characters"
                : "Mã DDC tài liệu không vượt quá 50 ký tự")
            .Must(str => DeweyDecimalUtils.IsValidDeweyDecimal(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "DDC number is not valid"
                : "Mã DDC tài liệu không hợp lệ");
        // Cutter number
        RuleFor(e => e.CutterNumber)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Cutter number must not exceed 50 characters"
                : "Ký hiệu xếp giá không vượt quá 50 ký tự")
            .Must(str => DeweyDecimalUtils.IsValidCutterNumber(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Cutter number is not valid"
                : "Ký hiệu xếp giá không hợp lệ");
        // Topical terms
        RuleFor(e => e.TopicalTerms)
            .MaximumLength(500)
            .WithMessage(isEng
                ? "Topical terms must not exceed 500 characters"
                : "Chủ đề có kiểm soát không vượt quá 500 ký tự");
        // Fullname
        RuleFor(u => u.Author)
            .NotEmpty()
            .WithMessage(isEng 
                ? "Fullname is required" 
                : "Họ và tên tác giả không được rỗng")
            .Length(1, 200)
            .WithMessage(isEng 
                ? "Fullname must be between 1 and 200 characters long" 
                : "Họ và tên phải có độ dài từ 1 đến 200 ký tự");
        
    }
}