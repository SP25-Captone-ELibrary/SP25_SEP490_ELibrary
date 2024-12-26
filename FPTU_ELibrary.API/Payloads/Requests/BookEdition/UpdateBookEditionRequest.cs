namespace FPTU_ELibrary.API.Payloads.Requests.BookEdition;

public class UpdateBookEditionRequest
{
    public string? EditionTitle { get; set; }
    public string? EditionSummary { get; set; }
    public int EditionNumber { get; set; }
    public int PageCount { get; set; }
    public string Language { get; set; } = null!;
    public int PublicationYear { get; set; }
    public string? CoverImage { get; set; }
    public string? Format { get; set; }
    public string? Publisher { get; set; }
    public string Isbn { get; set; } = null!;
    public bool CanBorrow { get; set; }
    public decimal EstimatedPrice { get; set; }
    public int? ShelfId { get; set; }
    public List<int> AuthorIds { get; set; } = new();
}