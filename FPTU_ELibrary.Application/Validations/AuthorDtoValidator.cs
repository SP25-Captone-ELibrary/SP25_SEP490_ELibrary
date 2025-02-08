using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class AuthorDtoValidator : AbstractValidator<AuthorDto>
{
    public AuthorDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        // Author Code
        RuleFor(u => u.AuthorCode)
            .Matches(@"^[A-Z]{2,4}\d{0,8}$")
            .WithMessage(isEng 
                ? "Author code must start with two to four uppercase letters, contain only digits after that, and be less than 10 characters" 
                : "Mã tác giả phải bắt đầu bằng hai đến bốn chữ cái viết hoa, chỉ chứa các chữ số sau đó và có độ dài nhỏ hơn 10 ký tự");
        // Author image
        RuleFor(e => e.AuthorImage)
            .Must(str => string.IsNullOrEmpty(str) || StringUtils.IsValidUrl(str))
            .WithMessage(isEng
                ? "Invalid author image"
                : "Hình ảnh đại diện không hợp lệ");
        // Biography
        RuleFor(e => e.Biography)
            .MaximumLength(2000)
            .WithMessage(isEng
                ? "Biography must not exceed 50 characters"
                : "Tiểu sử tác giả không vượt quá 50 ký tự");
        // Fullname
        RuleFor(u => u.FullName)
            .NotEmpty()
            .WithMessage(isEng 
                ? "Fullname is required" 
                : "Họ và tên tác giả không được rỗng")
            // .Matches(@"^[\p{L}\s'.-]+$")
            // .WithMessage(isEng 
            //     ? "Fullname should start with an uppercase letter for each word" 
            //     : "Họ và tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
            .Length(1, 200)
            .WithMessage(isEng 
                ? "Fullname must be between 1 and 200 characters long" 
                : "Họ và tên phải có độ dài từ 1 đến 200 ký tự");
        // Dob
        RuleFor(u => u.Dob)
            .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
            .WithMessage(isEng  
                ? "Invalid date of birth" 
                : "Ngày sinh không hợp lệ")
            .Must(dob => !dob.HasValue || DateTimeUtils.IsOver18(dob.Value))
            .WithMessage(isEng  
                ? "Author age must be over 18" 
                : "Tuổi tác giả yêu cầu lớn hơn 18");
        // Date of death
        RuleFor(u => u.DateOfDeath)
            .Must((u, dod) => !dod.HasValue || 
                              (u.Dob.HasValue && dod.Value.Date > u.Dob.Value.Date && dod.Value.Date < DateTime.Now.Date))
            .WithMessage(isEng 
                ? "Date of death must be greater than date of birth and less than the current date" 
                : "Ngày mất phải lớn hơn ngày sinh và nhỏ hơn ngày hiện tại");
    }
}