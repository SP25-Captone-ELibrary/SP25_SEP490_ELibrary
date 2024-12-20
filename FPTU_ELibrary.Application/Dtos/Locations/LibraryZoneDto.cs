using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryZoneDto
{
    // Key
    public int ZoneId { get; set; }

    // Floor belongs to
    public int FloorId { get; set; }
    
    // Zone detail
    public string? ZoneName { get; set; }
    public double XCoordinate { get; set; }
    public double YCoordinate { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    public LibraryFloorDto Floor { get; set; } = null!;

    [JsonIgnore]
    public ICollection<LibraryPathDto> LibraryPathFromZones { get; set; } = new List<LibraryPathDto>();

    [JsonIgnore]
    public ICollection<LibraryPathDto> LibraryPathToZones { get; set; } = new List<LibraryPathDto>();

    [JsonIgnore]
    public ICollection<LibrarySectionDto> LibrarySections { get; set; } = new List<LibrarySectionDto>();
}