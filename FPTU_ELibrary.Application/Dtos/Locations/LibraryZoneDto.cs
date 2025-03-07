using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryZoneDto
{
    // Key
    public int ZoneId { get; set; }

    // Floor belongs to
    public int FloorId { get; set; }
    
    // Zone detail
    public string EngZoneName { get; set; } = null!;
    public string VieZoneName { get; set; } = null!;
    
    // Description 
    public string? EngDescription { get; set; }
    public string? VieDescription { get; set; }
    
    // Total count
    public int TotalCount { get; set; }
    
    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    [JsonIgnore]
    public LibraryFloorDto Floor { get; set; } = null!;

    public ICollection<LibrarySectionDto> LibrarySections { get; set; } = new List<LibrarySectionDto>();
}