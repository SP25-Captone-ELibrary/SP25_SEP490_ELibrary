using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionInventoryDto
{
    // Key
    public int BookEditionId { get; set; }

    // Inventory amount 
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int RequestCopies { get; set; }
    public int BorrowedCopies { get; set; }
    public int ReservedCopies { get; set; }

    // Mapping entity
    [JsonIgnore]
    public BookEditionDto BookEdition { get; set; } = null!;
}