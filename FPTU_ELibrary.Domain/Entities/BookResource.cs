using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class BookResource : IAuditableEntity
{
    // Key
    public int ResourceId { get; set; }

    // Resource of which book
    public int BookId { get; set; }

    // Resource detail information
    public string ResourceType { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public string FileFormat { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string? ProviderMetadata { get; set; }
    public bool IsDeleted { get; set; }
    
    // Creation, update datetime and employee who in charge of creation
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public Book Book { get; set; } = null!;
}
