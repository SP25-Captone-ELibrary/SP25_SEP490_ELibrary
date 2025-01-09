using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Validations
{
	public class BookDtoValidator : AbstractValidator<BookDto>
	{
		public BookDtoValidator(string langContext) 
		{
			var langEnum =
				(SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(langContext);
			var isEng = langEnum == SystemLanguage.English;
			
			// Book code
			RuleFor(b => b.BookCode)
				.NotEmpty()
				.WithMessage(isEng
					? "Book code is required"
					: "Yêu cầu nhập mã sách")
				.MaximumLength(100)
				.WithMessage(isEng
					? "Book code must not exceed than 150 characters"
					: "Mã sách phải nhỏ hơn 100 ký tự");
			// Book title
			RuleFor(b => b.Title)
				.NotEmpty()
				.WithMessage(isEng
					? "Title is required"
					: "Yêu cầu nhập tiêu đề")
				.MaximumLength(150)
				.WithMessage(isEng
					? "Book title must not exceed than 150 characters"
					: "Tiêu đề phải nhỏ hơn 150 ký tự");
			// Book subtitle
			RuleFor(b => b.SubTitle)
				.MaximumLength(100)
				.WithMessage(isEng
					? "Subtitle must not exceed 100 characters"
					: "Tiêu đề phụ không vượt quá 100 ký tự");
			// Book summary
			RuleFor(b => b.Summary)
				.MaximumLength(2000)
				.WithMessage(isEng
					? "Summary must not exceed 2000 characters"
					: "Mô tả không vượt quá 2000 ký tự");
			// Book categories
			RuleFor(b => b.BookCategories)
				.NotNull()
				.WithMessage(isEng
					? "Book categories are required"
					: "Vui lòng chọn thể loại cho sách")
				.Must(b => b.Count > 0)
				.WithMessage(isEng
					? "Please select at least one book category"
					: "Vui lòng chọn ít nhất 1 thể loại cho sách");
			// Book editions
			RuleFor(b => b.BookEditions)
				.Must(be => be.Count > 0)
				.WithMessage(isEng
					? "Please add at least one book edition"
					: "Vui lòng tạo ít nhất 1 ấn bản")
				// Each edition
				.ForEach(edition =>
				{
					// Add edition validator
					edition.SetValidator(new BookEditionDtoValidator(langContext));
				});
			// Book resources
			RuleFor(e => e.BookResources)
				// Each edition resource
				.ForEach(src =>
				{
					// Add edition resource validator
					src.SetValidator(new BookResourceDtoValidator(langContext));
				});
		}
	}
}
