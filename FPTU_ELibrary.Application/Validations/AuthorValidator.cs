using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Authors;
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
        // FirstName
        RuleFor(u => u.FullName)
            .NotEmpty()
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").WithMessage("'{PropertyName}' should start with an uppercase letter for each word.")
            .Length(1, 200);
        // Dob
        RuleFor(u => u.Dob)
            .Must(dob => !dob.HasValue || DateTimeUtils.IsValidAge(dob.Value))
            .WithMessage("'Invalid date of birth'");
        // Date of death
        RuleFor(u => u.DateOfDeath)
            .Must(dod => !dod.HasValue || dod.Value.Date < DateTime.Now.Date)
            .WithMessage("'Invalid date of death'");
    }
}