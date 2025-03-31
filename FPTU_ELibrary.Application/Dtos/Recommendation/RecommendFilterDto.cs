namespace FPTU_ELibrary.Application.Dtos.Recommendation;

public class RecommendFilterDto
{
    public bool IncludeTitle { get; set; } = false; // Title
    public bool IncludeAuthor { get; set; } = false; // AuthorName, CutterNumber
    public bool IncludeGenres { get; set; } = false; // DDC
    public bool IncludeTopicalTerms { get; set; } = false; // TopicalTerms
    public bool LimitWorksOfAuthor { get; set; } = true; // Default is limit author's work
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
}