using System.Text.RegularExpressions;
using FluentValidation;
using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.Application.Validations;

public class BookCategoryDtoValidator : AbstractValidator<BookCategoryDto>
{
    public BookCategoryDtoValidator()
    {
        RuleFor(bc => bc.EnglishName)
            .NotEmpty().WithMessage("EnglishName is required.")
            .NotNull().WithMessage("EnglishName cannot be null.")
            .Matches(@"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$")
            .WithMessage("English name should start with an uppercase letter for each word.");
        RuleFor(bc => bc.VietnameseName)
            .NotEmpty().WithMessage("VietnameseName is required.")
            .NotNull().WithMessage("VietnameseName cannot be null.")
            .Matches(@"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$")
            .WithMessage("VietnameseName should start with an uppercase letter for each word.");
        
    }
}