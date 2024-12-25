using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class LearningMaterialDto
{
    // Key
    public int LearningMaterialId { get; set; }
    
    // Learning material detail information
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string MaterialType { get; set; } = null!;
    public string? Status { get; set; }
    public string? Condition { get; set; }
    
    // Which shelf belongs to 
    public int? ShelfId { get; set; }

    // Inventory amount
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }

    // Material detail
    public string? Manufacturer { get; set; }
    public DateOnly? WarrantyPeriod { get; set; }

    // Creation and update datetime, employee
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // Mapping entities
    public LibraryShelfDto? Shelf { get; set; }

    [JsonIgnore]
    public ICollection<BorrowRecordDto> BorrowRecords { get; set; } = new List<BorrowRecordDto>();

    [JsonIgnore]
    public ICollection<BorrowRequestDto> BorrowRequests { get; set; } = new List<BorrowRequestDto>();
}