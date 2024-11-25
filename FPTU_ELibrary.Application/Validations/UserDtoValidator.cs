using FluentValidation;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Utils;
using System.Text.RegularExpressions;

namespace FPTU_ELibrary.Application.Validations
{
	public class UserDtoValidator : AbstractValidator<UserDto>
	{
        public UserDtoValidator()
        {
			// UserCode
			RuleFor(u => u.UserCode)
				.Matches(@"^[A-Z]{2}\d{0,8}$")
				.WithMessage("'{PropertyName}' must start with two uppercase letters, contain only digits after that, and be less than 10 characters.");
            // Email
            RuleFor(u => u.Email)
                .EmailAddress()
                .NotNull();
			// FirstName
			RuleFor(u => u.FirstName)
				//.NotEmpty()
				.Matches(@"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$").WithMessage("'{PropertyName}' should start with an uppercase letter for each word.")
				.Length(3, 15);
			// LastName
			RuleFor(u => u.LastName)
				//.NotEmpty()
				.Matches(@"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$").WithMessage("'{PropertyName}' should start with an uppercase letter for each word.")
				.Length(3, 15);
			// Dob
			RuleFor(u => u.Dob)
				.Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
				.WithMessage("'Invalid date of birth'");
			// Phone
			RuleFor(p => p.Phone)
			   .Cascade(CascadeMode.Stop) // Prevent further checks if null/empty
			   .MinimumLength(10).WithMessage("'{PropertyName}' must not be less than 10 characters.")
			   .MaximumLength(15).WithMessage("'{PropertyName}' must not exceed 15 characters.")
			   .Matches(new Regex(@"^0\d{9,10}$")).WithMessage("'{PropertyName}' not valid");
		}
	}
}
