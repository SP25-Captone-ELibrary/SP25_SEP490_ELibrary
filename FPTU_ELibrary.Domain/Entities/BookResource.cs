using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookResource
{
    // Key
    public int ResourceId { get; set; }

    // Resource of which edition
    public int BookEditionId { get; set; }

    // Resource detail information
    public string ResourceType { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public string FileFormat { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string? ProviderMetadata { get; set; }
    
    // Creation, update datetime and employee who in charge of creation
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public Guid CreatedBy { get; set; }

    // Mapping entities
    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;

    public Employee CreatedByNavigation { get; set; } = null!;
}
