namespace FPTU_ELibrary.Application.Dtos.Recommendation;

// [Recommendation Systems - Standford InfoLab](http://infolab.stanford.edu/~ullman/mmds/ch9.pdf)
// [9.2.5 User Profiles]

/// <summary>
///  Estimate we can make regarding which items the user likes is some aggregation of the profiles of those items
/// </summary>
public class UserProfileActivity
{
    public Guid UserId { get; set; }
    public int LibraryItemId { get; set; }

    // Explicit feedback rating (from 1 to 5, with > 3 considered positive feedback)
    public double Rating { get; set; } 
    
    // Implicit feedback flags indicating actual consumption and interest
    public bool Borrowed { get; set; }
    public bool Reserved { get; set; }
    public bool Favorite { get; set; }
    
    // Additional fields
    public int BorrowCount { get; set; } = 0;
    public int ReserveCount { get; set; } = 0;
}