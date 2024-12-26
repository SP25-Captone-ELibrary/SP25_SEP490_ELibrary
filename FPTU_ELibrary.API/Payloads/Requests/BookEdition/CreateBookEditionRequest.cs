using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookEditionRequest
{
    public string? EditionTitle { get; set; } = null!;
    public int EditionNumber { get; set; }
    public string? EditionSummary { get; set; }
    public int PageCount { get; set; }
    public string Language { get; set; } = null!;
    public int PublicationYear { get; set; }
    public string BookFormat { get; set; } = null!;
    public string CoverImage { get; set; } = null!;
    public string? Publisher { get; set; }
    public string Isbn { get; set; } = null!;
    public decimal EstimatedPrice { get; set; }
    
    // Copies  
    public List<CreateBookEditionCopyRequest>? BookCopies { get; set; } = new();
    
    // Book authors
    public List<int> AuthorIds { get; set; } = new();
}
