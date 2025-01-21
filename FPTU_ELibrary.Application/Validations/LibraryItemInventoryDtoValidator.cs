using FluentValidation;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Validations;

public class LibraryItemInventoryDtoValidator : AbstractValidator<LibraryItemInventoryDto>
{
    public LibraryItemInventoryDtoValidator()
    {
    }
}