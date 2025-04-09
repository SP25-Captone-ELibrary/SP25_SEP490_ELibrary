using FluentValidation;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class SupplementRequestDetailDtoValidator : AbstractValidator<SupplementRequestDetailDto>
{
    public SupplementRequestDetailDtoValidator(string langContext)
    {
        // Determine the language based on the provided context
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = lang == SystemLanguage.English;
        
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
        // Author
        RuleFor(e => e.Author)
            .NotEmpty()
            .WithMessage(isEng 
                ? "Author is required" 
                : "Tác giả không được rỗng")
            .Length(1, 255)
            .WithMessage(isEng 
                ? "Author must be between 1 and 255 characters" 
                : "Tác giả phải có từ 1 đến 255 ký tự");

        // Publisher
        RuleFor(e => e.Publisher)
            .NotEmpty()
            .WithMessage(isEng 
                ? "Publisher is required" 
                : "Nhà xuất bản không được rỗng")
            .Length(1, 155)
            .WithMessage(isEng 
                ? "Publisher must be between 1 and 155 characters" 
                : "Nhà xuất bản phải có từ 1 đến 155 ký tự");
        
        // Published Date 
        RuleFor(e => e.PublishedDate)
            .MaximumLength(50)
            .WithMessage(isEng 
                ? "Published date must not exceed 50 characters" 
                : "Ngày xuất bản không vượt quá 50 ký tự");

        // Description
        RuleFor(e => e.Description)
            .MaximumLength(3000)
            .WithMessage(isEng 
                ? "Description must not exceed 3000 characters" 
                : "Mô tả không vượt quá 3000 ký tự");

        // Edition summary
        RuleFor(e => e.SupplementRequestReason)
            .MaximumLength(255)
            .WithMessage(isEng
                ? "Supplement request reason must not exceed 255 characters"
                : "Lý do yêu cầu nhập không vượt quá 255 ký tự");
        
        // ISBN 
        RuleFor(e => e.Isbn)
            .Must(str => string.IsNullOrEmpty(str) || (ISBN.IsValid(str, out _) && ISBN.CleanIsbn(str).Length <= 13))
            .WithMessage(isEng 
                ? "ISBN is not valid" 
                : "Mã ISBN không hợp lệ");

        // Page Count
        RuleFor(e => e.PageCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage(isEng 
                ? "Page count must be zero or greater" 
                : "Số trang phải lớn hơn hoặc bằng 0");

        // Dimensions
        RuleFor(e => e.Dimensions)
            .MaximumLength(155)
            .WithMessage(isEng 
                ? "Dimensions must not exceed 155 characters" 
                : "Kích thước không vượt quá 155 ký tự");
        
        // Categories
        RuleFor(e => e.Categories)
            .MaximumLength(255)
            .WithMessage(isEng 
                ? "Categories must not exceed 255 characters" 
                : "Danh mục không vượt quá 255 ký tự");
        
        // Average Rating
        RuleFor(e => e.AverageRating)
            .InclusiveBetween(0, 5)
            .When(e => e.AverageRating.HasValue)
            .WithMessage(isEng 
                ? "Average rating must be between 0 and 5" 
                : "Đánh giá trung bình phải nằm trong khoảng từ 0 đến 5");

        // Ratings Count
        RuleFor(e => e.RatingsCount)
            .GreaterThanOrEqualTo(0)
            .When(e => e.RatingsCount.HasValue)
            .WithMessage(isEng 
                ? "Ratings count must be zero or greater" 
                : "Số lượt đánh giá phải lớn hơn hoặc bằng 0");

        // Language
        RuleFor(e => e.Language)
            .MaximumLength(50)
            .WithMessage(isEng 
                ? "Language must not exceed 50 characters" 
                : "Ngôn ngữ không vượt quá 50 ký tự");
        
        // Validate cover image Url
        RuleFor(e => e.CoverImageLink)
            .Must(str => string.IsNullOrEmpty(str) || StringUtils.IsValidUrl(str))
            .WithMessage(isEng 
                ? "Cover image is invalid" 
                : "Hình ảnh bìa không hợp lệ");
        RuleFor(e => e.PreviewLink)
            .Must(str => string.IsNullOrEmpty(str) || StringUtils.IsValidUrl(str))
            .WithMessage(isEng 
                ? "Preview link is invalid" 
                : "Liên kết xem trước không hợp lệ");
        RuleFor(e => e.InfoLink)
            .Must(str => string.IsNullOrEmpty(str) || StringUtils.IsValidUrl(str))
            .WithMessage(isEng 
                ? "Info link is invalid" 
                : "Đường dẫn liên kêt không hợp lệ");
    }
}