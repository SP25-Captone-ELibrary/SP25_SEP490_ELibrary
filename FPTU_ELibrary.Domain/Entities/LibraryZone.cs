using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryZone
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

    public LibraryFloor Floor { get; set; } = null!;

    [JsonIgnore]
    public ICollection<LibraryPath> LibraryPathFromZones { get; set; } = new List<LibraryPath>();

    [JsonIgnore]
    public ICollection<LibraryPath> LibraryPathToZones { get; set; } = new List<LibraryPath>();

    [JsonIgnore]
    public ICollection<LibrarySection> LibrarySections { get; set; } = new List<LibrarySection>();
}
