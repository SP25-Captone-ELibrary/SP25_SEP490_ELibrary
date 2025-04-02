using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemInventoryDto
{
    // Key
    public int LibraryItemId { get; set; }

    // Inventory amount 
    public int TotalUnits { get; set; }
    public int AvailableUnits { get; set; }
    public int RequestUnits { get; set; }
    public int BorrowedUnits { get; set; }
    public int ReservedUnits { get; set; }
    public int LostUnits { get; set; }
    
    // Instances units
    public int TotalInShelfUnits { get; set; } = 0;
    public int TotalInWarehouseUnits { get; set; } = 0;

    // Mapping entity
    [JsonIgnore]
    public LibraryItemDto LibraryItem { get; set; } = null!;
}