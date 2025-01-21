using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Utils;
using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations
{
	public class UserDtoValidator : AbstractValidator<UserDto>
	{
        public UserDtoValidator(string langContext)
        {
			var langEnum =
                (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
            var isEng = langEnum == SystemLanguage.English;

            // Email
            RuleFor(u => u.Email)
                .EmailAddress()
                .WithMessage(isEng ? "Not valid email address" : "Email không hợp lệ");
            // FirstName
            RuleFor(u => u.FirstName)
                .NotEmpty()
                .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
                .WithMessage(isEng 
                    ? "Firstname should start with an uppercase letter for each word" 
                    : "Họ phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
                .Length(1, 100)
                .WithMessage(isEng 
                    ? "Firstname must be between 1 and 100 characters long" 
                    : "Họ phải có độ dài từ 1 đến 100 ký tự");
            // LastName
            RuleFor(u => u.LastName)
                .NotEmpty()
                .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
                .WithMessage(isEng 
                    ? "Lastname should start with an uppercase letter for each word" 
                    : "Tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ")
                .Length(1, 100)
                .WithMessage(isEng 
                    ? "Lastname must be between 1 and 100 characters long" 
                    : "Tên phải có độ dài từ 1 đến 100 ký tự");
            // Dob
            RuleFor(u => u.Dob)
                .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
                .WithMessage(isEng 
                    ? "Invalid date of birth" 
                    : "Ngày sinh không hợp lệ");
            // Phone
            RuleFor(p => p.Phone)
                .Cascade(CascadeMode.Stop) // Prevent further checks if null/empty
                .MinimumLength(10)
                .WithMessage(isEng 
                    ? "Phone must not be less than 10 characters" 
                    : "SĐT không được ít hơn 10 ký tự")
                .MaximumLength(12)
                .WithMessage(isEng 
                    ? "Phone must not exceed 12 characters" 
                    : "SĐT không được vượt quá 12 ký tự")
                .Matches(new Regex(@"^0\d{9,10}$"))
                .WithMessage(isEng 
                    ? "Phone not valid" 
                    : "SĐT không hợp lệ");
		}
	}
}
