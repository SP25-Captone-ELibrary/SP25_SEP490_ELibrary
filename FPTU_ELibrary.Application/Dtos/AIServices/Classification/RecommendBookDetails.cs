using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class RecommendBookDetails
{
    public LibraryItemDetailDto ItemDetailDto  { get; set; }
    public List<MatchedProperties> MatchedProperties { get; set; }
}

public class MatchedProperties
{
    public string Name { get; set; }
    public bool IsMatched { get; set; }
}

