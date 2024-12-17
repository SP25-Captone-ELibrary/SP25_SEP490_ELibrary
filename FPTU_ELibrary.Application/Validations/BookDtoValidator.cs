using FluentValidation;
using FluentValidation.Results;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.Application.Validations
{
	public class BookDtoValidator : AbstractValidator<BookDto>
	{
		public BookDtoValidator() 
		{
			RuleFor(b => b.Title)
				.NotNull()
				.MaximumLength(100)
				.WithMessage("Book title must greater than 100 characters");
		}
	}
}
