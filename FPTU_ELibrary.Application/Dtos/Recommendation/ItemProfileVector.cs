namespace FPTU_ELibrary.Application.Dtos.Recommendation;

// [Recommendation Systems - Standford InfoLab](http://infolab.stanford.edu/~ullman/mmds/ch9.pdf)
// [9.2.1 Item Profiles]

/// <summary>
/// Construct for each item a 'profile', which represents the important characteristics of that item.
/// 'Profile' consists of some characteristics of the item that are easily discovered (title, subtitle, description, genres, topical terms).
/// Terminology term: [Feature Vector]
/// </summary>
public class ItemProfileVector
{
    // For specific item
    public int LibraryItemId { get; set; }

    // A dictionary mapping each term to its TF-IDF weight
    public Dictionary<string, double> TfidfVector { get; set; } = new();
}