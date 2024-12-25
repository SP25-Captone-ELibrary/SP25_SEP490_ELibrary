using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
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
        // Fullname
        RuleFor(u => u.FullName)
            .NotEmpty()
            .Matches(@"^[\p{L}\s'.-]+$")
            .WithMessage(isEng 
                ? "Fullname should start with an uppercase letter for each word" 
                : "Họ và tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
            .Length(1, 200)
            .WithMessage(isEng 
                ? "Fullname must be between 1 and 200 characters long" 
                : "Họ và tên phải có độ dài từ 1 đến 200 ký tự");
        // Dob
        RuleFor(u => u.Dob)
            .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
            .WithMessage(isEng 
                ? "Invalid date of birth" 
                : "Ngày sinh không hợp lệ");
        // Date of death
        // TODO: Add constraint with DOB
        RuleFor(u => u.DateOfDeath)
            .Must(dod => !dod.HasValue || dod.Value.Date < DateTime.Now.Date)
            .WithMessage(isEng 
                    ? "Invalid date of death" 
                    : "Ngày mất không hợp lệ");
    }
}