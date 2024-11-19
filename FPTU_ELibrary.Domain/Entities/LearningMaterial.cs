using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LearningMaterial
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
    public DateTime CreateDate { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Mapping entities
    public LibraryShelf? Shelf { get; set; }

    [JsonIgnore]
    public Employee CreateByNavigation { get; set; } = null!;

    [JsonIgnore]
    public Employee? UpdatedByNavigation { get; set; }
    
    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
}
