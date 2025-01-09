using FluentValidation;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class BookEditionDtoValidator : AbstractValidator<BookEditionDto>
{
    public BookEditionDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Edition title 
        RuleFor(e => e.EditionTitle)
            .MaximumLength(150)
            .WithMessage(isEng
                ? "Book edition title must not exceed than 150 characters"
                : "Tiêu đề của ấn bản phải nhỏ hơn 150 ký tự");
        // Edition summary
        RuleFor(e => e.EditionSummary)
            .MaximumLength(500)
            .WithMessage(isEng
                ? "Edition summary must not exceed 500 characters"
                : "Mô tả của ấn bản không vượt quá 500 ký tự");
        // Edition number
        RuleFor(e => e.EditionNumber)
            .Must(num => num > 0 && num < int.MaxValue)
            .WithMessage(isEng
                ? "Book edition number is not valid"
                : "Số thứ tự ấn bản không hợp lệ");
        // Language
        RuleFor(e => e.Language)
            .Must(str => !StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Language is not valid"
                : "Ngôn ngữ không hợp lệ");
        // Book format
        RuleFor(e => e.Format)
            .Must(str => !string.IsNullOrEmpty(str) 
                         && !StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str)
                         && Enum.TryParse(typeof(BookFormat), str, true, out _))
            .WithMessage(isEng
                ? "Book format is not valid"
                : "Format sách không hợp lệ");
        // Page count
        RuleFor(e => e.PageCount)
            .Must(num => num > 0 && num < int.MaxValue)
            .WithMessage(isEng
                ? "Page count is not valid"
                : "Tổng số trang không hợp lệ");
        // Publication year
        RuleFor(e => e.PublicationYear)
            .Must(num => IsValidYear(num.ToString()))
            .WithMessage(isEng
                ? "Publication year is not valid"
                : "Năm xuất bản không hợp lệ");
        // Publisher
        RuleFor(e => e.Publisher)
            .Must(str => !string.IsNullOrEmpty(str)
                         && !StringUtils.IsNumeric(str) && !StringUtils.IsDateTime(str))
            .WithMessage(isEng
                ? "Publisher is not valid"
                : "Tên nhà xuất bản không hợp lệ");
        // Isbn
        RuleFor(e => e.Isbn)
            .Must(str => ISBN.IsValid(str, out _))
            .WithMessage(isEng
                ? "ISBN is not valid"
                : "Mã ISBN không hợp lệ");
        // Estimated Price
        RuleFor(e => e.EstimatedPrice)
            .InclusiveBetween(1000, 9999999999)
            .WithMessage(e => 
            {
                if (e.EstimatedPrice < 1000)
                {
                    return isEng
                        ? "EstimatedPrice must be at least 1.000 VND"
                        : "Giá phải ít nhất là 1.000 VND";
                }
                else if (e.EstimatedPrice > 9999999999)
                {
                    return isEng
                        ? "EstimatedPrice exceeds the maximum limit of 9.999.999.999 VND"
                        : "Giá vượt quá giới hạn tối đa là 9.999.999.999 VND";
                }

                // Default message (shouldn't occur because of the Must condition)
                return isEng
                    ? "Invalid EstimatedPrice value"
                    : "Giá trị EstimatedPrice không hợp lệ";
            });
        // Validate cover image Url
        RuleFor(e => e.CoverImage)
            .Must(str => !string.IsNullOrEmpty(str) && StringUtils.IsValidUrl(str))
            .WithMessage(isEng
                ? "Invalid book cover image"
                : "Hình ảnh không hợp lệ");
        // Validate book authors
        RuleFor(e => e.BookEditionAuthors)
            .Must(ba => ba.Count > 0)
            .WithMessage(isEng
                ? "Please add at least one author"
                : "Vui lòng thêm tác giả");
        // Validate copies
        RuleFor(e => e.BookEditionCopies)
            // Each copy edition
            .ForEach(copy =>
            {
                // Add edition copy validator
                copy.SetValidator(new BookEditionCopyDtoValidator(langContext));
            });
    }
    
    private bool IsValidYear(string text) => int.TryParse(text, out var year) && year > 0 && year <= DateTime.Now.Year;
}