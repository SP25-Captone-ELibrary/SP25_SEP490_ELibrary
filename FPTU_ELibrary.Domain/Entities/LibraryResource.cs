using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryResource : IAuditableEntity
{
    // Key
    public int ResourceId { get; set; }

    // Resource detail information
    public string ResourceTitle { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public string FileFormat { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string? ProviderMetadata { get; set; }
    public bool IsDeleted { get; set; }
    public int DefaultBorrowDurationDays { get; set; }
    public decimal BorrowPrice { get; set; }
    
    // Creation, update datetime and employee who in charge of creation
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    [JsonIgnore]
    public ICollection<LibraryItemResource> LibraryItemResources { get; set; } = new List<LibraryItemResource>();
    
    [JsonIgnore]
    public ICollection<DigitalBorrow> DigitalBorrows { get; set; } = new List<DigitalBorrow>();

    [JsonIgnore] 
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
