using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemGroupDtoValidator : AbstractValidator<LibraryItemGroupDto>
{ 
    public LibraryItemGroupDtoValidator(string language)
    {
    }
}