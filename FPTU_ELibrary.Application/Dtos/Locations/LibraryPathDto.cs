using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryPathDto
{
    // Key
    public int PathId { get; set; }

    // Path detail
    public int FromZoneId { get; set; }
    public int ToZoneId { get; set; }
    public double Distance { get; set; }
    public string? PathDescription { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    // Mapping entities
    [JsonIgnore]
    public LibraryZoneDto FromZone { get; set; } = null!;

    [JsonIgnore]
    public LibraryZoneDto ToZone { get; set; } = null!;
}