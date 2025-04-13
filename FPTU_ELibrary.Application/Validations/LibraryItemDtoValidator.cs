using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemDtoValidator : AbstractValidator<LibraryItemDto>
{
    public LibraryItemDtoValidator(string langContext)
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
        // Responsibility 
        RuleFor(e => e.Responsibility)
            .MaximumLength(155)
            .WithMessage(isEng
                ? "Statement of responsibility must not exceed than 155 characters"
                : "Thông tin trách nhiệm của tài liệu phải nhỏ hơn 155 ký tự");
        // Edition 
        RuleFor(e => e.Edition)
            .MaximumLength(100)
            .WithMessage(isEng
                ? "Edition/reprint must not exceed than 100 characters"
                : "Lần xuất bản/tái bản phải nhỏ hơn 100 ký tự");
        // Edition number
        RuleFor(e => e.EditionNumber)
            .Must(num => num == null || num > 0 && num < int.MaxValue)
            .WithMessage(isEng
                ? "Book edition number is not valid"
                : "Số thứ tự tài liệu không hợp lệ");
        // Language
        RuleFor(e => e.Language)
            .NotEmpty()
            .WithMessage(isEng
                ? "Language is required" 
                : "Yêu cầu xác định ngôn ngữ cho tài liệu")
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Language code must not exceed than 50 characters"
                : "Mã ngôn ngữ phải nhỏ hơn 50 ký tự")
            .Must(str => !StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Language is not valid"
                : "Ngôn ngữ không hợp lệ");
        // Origin Language
        RuleFor(e => e.OriginLanguage)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Language code must not exceed than 50 characters"
                : "Mã ngôn ngữ phải nhỏ hơn 50 ký tự")
            .Must(str => !StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Language is not valid"
                : "Ngôn ngữ không hợp lệ");
        // Edition summary
        RuleFor(e => e.Summary)
            .MaximumLength(500)
            .WithMessage(isEng
                ? "Item summary must not exceed 500 characters"
                : "Mô tả của tài liệu không vượt quá 500 ký tự");
        // Validate cover image Url
        RuleFor(e => e.CoverImage)
            .Must(str => !string.IsNullOrEmpty(str) && StringUtils.IsValidUrl(str))
            .WithMessage(isEng
                ? "Invalid book cover image"
                : "Hình ảnh không hợp lệ");
        // Publication year
        RuleFor(e => e.PublicationYear)
            .Must(num => IsValidYear(num.ToString()))
            .WithMessage(isEng
                ? "Publication year is not valid"
                : "Năm xuất bản không hợp lệ");
        // Publisher
        RuleFor(e => e.Publisher)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Publisher must not exceed 255 characters"
                : "Tên nhà xuất bản không vượt quá 255 ký tự")
            .Must(str => string.IsNullOrEmpty(str)
                         || !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Publisher is not valid"
                : "Tên nhà xuất bản không hợp lệ");
        // Publication place
        RuleFor(e => e.PublicationPlace)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Publication place must not exceed 255 characters"
                : "Nơi xuất bản không vượt quá 255 ký tự")
            .Must(str => string.IsNullOrEmpty(str)
                         || (!StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str)))
            .WithMessage(isEng
                ? "Publication place is not valid"
                : "Nơi xuất bản không hợp lệ");
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
        // Isbn
        RuleFor(e => e.Isbn)
            .Must(str => str == null || (ISBN.IsValid(str, out _) && ISBN.CleanIsbn(str).Length <= 13))
            .WithMessage(isEng
                ? "ISBN is not valid"
                : "Mã ISBN không hợp lệ");
        // Ean
        RuleFor(e => e.Ean)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Other standard identifier must not exceed 50 characters"
                : "Chỉ số khác (EAC, số XB, v.v.) không vượt quá 50 ký tự");
        // Estimated Price
        RuleFor(e => e.EstimatedPrice)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.EstimatedPrice < 1000)
                {
                    return isEng
                        ? "Estimated price must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND";
                }
                else if (e.EstimatedPrice > 9999999999)
                {
                    return isEng
                        ? "Estimated price exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }
        
                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid Estimated price value"
                    : "Giá tiền ước tính không hợp lệ";
            });
        // Page count
        RuleFor(e => e.PageCount)
            .Must(num => num > 0 && num < int.MaxValue)
            .WithMessage(isEng
                ? "Page count is not valid"
                : "Tổng số trang không hợp lệ");
        // Physical details
        RuleFor(e => e.PhysicalDetails)
            .MaximumLength(100)
            .WithMessage(isEng
                ? "Physical detail must not exceed 100 characters"
                : "Các đặc điểm vật lý khác không vượt quá 100 ký tự");
        // Dimensions
        RuleFor(e => e.Dimensions)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Dimensions must not exceed 50 characters"
                : "Mô tả kích thước không vượt quá 50 ký tự");
        // Accompanying material
        RuleFor(e => e.AccompanyingMaterial)
            .MaximumLength(50)
            .WithMessage(isEng
                ? "Accompanying material must not exceed 50 characters"
                : "Tài liệu kèm theo không vượt quá 50 ký tự");
        // Genres
        RuleFor(e => e.Genres)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Genres must not exceed 255 characters"
                : "Chủ đề thể loại/hình thức không vượt quá 255 ký tự");
        // General note
        RuleFor(e => e.GeneralNote)
            .MaximumLength(100)
            .WithMessage(isEng
                ? "General note must not exceed 100 characters"
                : "Phụ chú chung không vượt quá 100 ký tự");
        // Bibliographical note
        RuleFor(e => e.BibliographicalNote)
            .MaximumLength(100)
            .WithMessage(isEng
                ? "Bibliographical note must not exceed 100 characters"
                : "Phụ chú thư mục không vượt quá 100 ký tự");
        // Topical terms
        RuleFor(e => e.TopicalTerms)
            .MaximumLength(500)
            .WithMessage(isEng
                ? "Topical terms must not exceed 500 characters"
                : "Chủ đề có kiểm soát không vượt quá 500 ký tự");
        // Additional authors
        RuleFor(e => e.AdditionalAuthors)
            .MaximumLength(500)
            .WithMessage(isEng
                ? "Additional authors must not exceed 500 characters"
                : "Tác giả bổ sung không vượt quá 500 ký tự");
        
        // Validate instance
        RuleFor(e => e.LibraryItemInstances)
            // Each instance
            .ForEach(copy =>
            {
                // Add item instance validator
                copy.SetValidator(new LibraryItemInstanceDtoValidator(langContext));
            });
    }
    
    private bool IsValidYear(string text) => int.TryParse(text, out var year) && year > 0 && year <= DateTime.Now.Year;
}