using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class DigitalBorrowExtensionHistory
{
    // Key
    public int DigitalExtensionHistoryId { get; set; }
    
    // For which digital borrow
    public int DigitalBorrowId { get; set; }
    
    // Extension details
    public DateTime ExtensionDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public decimal ExtensionFee { get; set; }
    public int ExtensionNumber { get; set; }
    
    // References
    [JsonIgnore] 
    public DigitalBorrow DigitalBorrow { get; set; } = null!;
}


// Custom comparer for the DigitalBorrowExtensionHistory class
public class DigitalBorrowExtensionHistoryComparer : IEqualityComparer<DigitalBorrowExtensionHistory>
{
    // DigitalBorrowExtensionHistories are equal if their id and userId are equal
    public bool Equals(DigitalBorrowExtensionHistory x, DigitalBorrowExtensionHistory y)
    {
        // Check whether the compared objects reference the same data
        if (Object.ReferenceEquals(x, y)) return true;

        // Check whether any of the compared objects is null
        if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
            return false;
 
        // Check whether the DigitalBorrowExtensionHistories' properties are equal
        return x.DigitalBorrowId == y.DigitalBorrowId && x.DigitalBorrow.UserId == y.DigitalBorrow.UserId;
    }
    
    // If Equals() returns true for a pair of objects
    // then GetHashCode() must return the same value for these objects.
    public int GetHashCode(DigitalBorrowExtensionHistory his)
    {
        // Check whether the object is null
        if (Object.ReferenceEquals(his, null)) return 0;

        // Get hash code for the UserId field if it is not null
        int hashUserId = his.DigitalBorrow == null! ? 0 : his.DigitalBorrow.UserId.GetHashCode();

        // Get hash code for the DigitalBorrowId field
        int hasDigitalBorrowId = his.DigitalBorrowId.GetHashCode();

        // Calculate the hash code for the DigitalBorrowExtensionHistory
        return hashUserId ^ hasDigitalBorrowId;
    }
}