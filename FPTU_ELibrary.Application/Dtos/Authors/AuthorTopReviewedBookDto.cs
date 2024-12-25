using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.Application.Dtos.Authors;

public class AuthorTopReviewedBookDto
{
    public BookEditionDto BookEdition { get; set; } = null!;
}