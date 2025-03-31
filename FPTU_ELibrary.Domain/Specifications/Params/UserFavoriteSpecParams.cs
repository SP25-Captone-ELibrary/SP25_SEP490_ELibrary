namespace FPTU_ELibrary.Domain.Specifications.Params;

public class UserFavoriteSpecParams :BaseSpecParams
{
    public bool? CanBorrow { get; set; }
    public DateTime?[]? CreatedAtRange { get; set; }
    
    // Basic search
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? ClassificationNumber { get; set; }
    public string? Genres { get; set; }
    public string? Publisher { get; set; }
    public string? TopicalTerms { get; set; }
}
