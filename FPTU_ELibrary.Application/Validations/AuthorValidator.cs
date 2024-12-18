using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Utils;

namespace FPTU_ELibrary.Application.Validations;

public class AuthorValidator : AbstractValidator<AuthorDto>
{
    public AuthorValidator()
    {
        // EmployeeCode
        RuleFor(u => u.AuthorCode)
            .Matches(@"^[A-Z]{2}\d{0,8}$")
            .WithMessage("'{PropertyName}' must start with two uppercase letters, contain only digits after that, and be less than 10 characters.");
        // Email
        RuleFor(u => u.Email)
            .EmailAddress()
            .NotNull();
        // FirstName
        RuleFor(u => u.FirstName)
            .NotEmpty()
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").WithMessage("'{PropertyName}' should start with an uppercase letter for each word.")
            .Length(1, 15);
        // LastName
        RuleFor(u => u.LastName)
            .NotEmpty()
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").WithMessage("'{PropertyName}' should start with an uppercase letter for each word.")
            .Length(1, 15);
        // Dob
        RuleFor(u => u.Dob)
            .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
            .WithMessage("'Invalid date of birth'");
    }
}