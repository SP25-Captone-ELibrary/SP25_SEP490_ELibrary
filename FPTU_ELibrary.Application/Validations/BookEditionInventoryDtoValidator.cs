using FluentValidation;
using FPTU_ELibrary.Application.Dtos.BookEditions;

namespace FPTU_ELibrary.Application.Validations;

public class BookEditionInventoryDtoValidator : AbstractValidator<BookEditionInventoryDto>
{
    public BookEditionInventoryDtoValidator()
    {
    }
}