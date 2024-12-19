using System.Text.RegularExpressions;
using FluentValidation;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;

public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;

        // EmployeeCode
        RuleFor(u => u.EmployeeCode)
            .Matches(@"^[A-Z]{2}\d{0,8}$")
            .WithMessage(isEng 
                ? "Employee code must start with two uppercase letters, contain only digits after that, and be less than 10 characters" 
                : "Mã nhân viên phải bắt đầu bằng hai chữ cái viết hoa, chỉ chứa các chữ số sau đó và có độ dài nhỏ hơn 10 ký tự");
        // Email
        RuleFor(u => u.Email)
            .Matches(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")
            .NotNull()
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
        // Validate TerminationDate
        RuleFor(u => u.TerminationDate)
            .Must((model, terminationDate) => !terminationDate.HasValue 
                                              || (model.HireDate.HasValue && terminationDate >= model.HireDate))
            .WithMessage(isEng
                ? "Termination date must be smaller than hire date"
                : "Ngày được thuê phải nhỏ hơn ngày nghỉ việc");
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