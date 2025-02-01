using System.Data;
using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations;
 
public class SupplierDtoValidator : AbstractValidator<SupplierDto>
{
    public SupplierDtoValidator(string langContext)
    {
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
        var isEng = langEnum == SystemLanguage.English;
        
        // Supplier name
        RuleFor(s => s.SupplierName)
            .Length(5, 255)
            .WithMessage(e => isEng
                ? "Supplier name must be between 5 and 255 characters"
                : "Độ dài tên nhà cung cấp phải nằm trong khoảng 5 đến 255 ký tự")
            .Matches("^[\\p{L}\\s]+$")
            .WithMessage(e => isEng
                ? "Supplier name must not contain special characters or numbers"
                : "Tên nhà cung cấp không được chứa ký tự đặc biệt hoặc số");
        // Contact person
        RuleFor(s => s.ContactPerson)
            .Length(5, 255)
            .WithMessage(e => isEng
                ? "Contact person must be between 5 and 255 characters"
                : "Độ dài thông tin người liên lạc phải nằm trong khoảng 5 đến 255 ký tự")
            .Matches("^[\\p{L}\\s]+$")
            .WithMessage(e => isEng
                ? "Contact person must not contain special characters or numbers"
                : "Thông tin người liên lạc không được chứa ký tự đặc biệt hoặc số");
        // Contact email
        RuleFor(e => e.ContactEmail)
            .MaximumLength(255)
            .WithMessage(e => isEng
                ? "Email must not exceed 255 characters"
                : "Độ dài địa chỉ email không vượt quá 255 ký tự")
            .EmailAddress()
            .WithMessage(isEng 
                ? "Wrong email address format"
                : "Địa chỉ email không hợp lệ");
        // Contact phone 
        RuleFor(e => e.ContactPhone)
            .Length(10, 12)
            .WithMessage(isEng
                ? "Phone length must be between 10 and 12 characters"
                : "Độ dài số điện thoại liên lạc nằm trong khoảng 10 đến 12 ký tự");
        // Address 
        RuleFor(e => e.Address)
            .MaximumLength(300)
            .WithMessage(e => isEng
                ? "Address must not exceed 300 characters"
                : "Địa chỉ không vượt quá 300 ký tự");
        // Country 
        RuleFor(e => e.Country)
            .MaximumLength(100)
            .WithMessage(e => isEng
                ? "Country must not exceed 100 characters"
                : "Đất nước không vượt quá 100 ký tự")
            .Matches("^[\\p{L}\\s]+$")
            .WithMessage(e => isEng
                ? "Country must not contain special characters or numbers"
                : "Đất nước không được chứa ký tự đặc biệt hoặc số");
        // City 
        RuleFor(e => e.City)
            .MaximumLength(100)
            .WithMessage(e => isEng
                ? "City must not exceed 100 characters"
                : "Thành phố không vượt quá 100 ký tự")
            .Matches("^[\\p{L}\\s]+$")
            .WithMessage(e => isEng
                ? "City must not contain special characters or numbers"
                : "Thành phố không được chứa ký tự đặc biệt hoặc số");
    }
}