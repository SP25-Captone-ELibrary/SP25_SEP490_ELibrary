namespace FPTU_ELibrary.Application.Dtos.Authors;

public class AuthorDetailDto
{
    public AuthorDto Author { get; set; } = null!;
    public int TotalPublishedBook { get; set; }
    public double UserReviews { get; set; }
    public List<AuthorTopReviewedBookDto> TopReviewedBooks { get; set; } = new();
}