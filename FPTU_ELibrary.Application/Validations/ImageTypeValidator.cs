using FluentValidation;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Validations;

public class ImageTypeValidator : AbstractValidator<IFormFile>
{
    public ImageTypeValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        RuleFor(file => file.ContentType)
            .NotEmpty()
            .Must(BeAValidFileType)
            .WithMessage(isEng
                ? "Please upload a valid file. Allowed types are: images, PDF, or common e-book formats (.epub, .mobi, etc.)."
                : "Vui lòng tải lên tệp hợp lệ. Các loại được phép là: hình ảnh, PDF hoặc các định dạng sách điện tử phổ biến (.epub, .mobi, v.v.).");
    }

    private bool BeAValidFileType(string contentType)
    {
        // Allowed MIME types
        var allowedTypes = new[]
        {
            // Image MIME types
            "image/jpeg", "image/png", "image/gif", "image/svg+xml", "image/bmp", "image/tiff", "image/x-icon",

            // PDF MIME type
            "application/pdf",

            // E-book MIME types
            "application/epub+zip", // EPUB
            "application/x-mobipocket-ebook", // MOBI
            "application/vnd.amazon.ebook", // AZW (Amazon Kindle format)
            "application/x-fictionbook+xml", // FB2
            "application/vnd.ms-htmlhelp", // CHM
            "text/plain", // TXT (plain text files, for simple e-books)
            "application/x-cbr", // CBR (Comic book format)
            "application/x-cbz", // CBZ (Comic book format in ZIP)
            "application/vnd.ms-excel.sheet.macroenabled.12" //xls
        };

        return allowedTypes.Contains(contentType);
    }
}