
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemInventory
{
    // Key
    public int LibraryItemId { get; set; }

    // Inventory amount 
    public int TotalUnits { get; set; }
    public int AvailableUnits { get; set; }
    public int RequestUnits { get; set; }
    public int BorrowedUnits { get; set; }
    public int ReservedUnits { get; set; }

    // Mapping entity
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;
}
