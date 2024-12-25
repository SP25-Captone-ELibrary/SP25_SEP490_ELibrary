using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibrarySectionDto
{
    // Key
    public int SectionId { get; set; }
    
    // Zone belongs to
    public int ZoneId { get; set; }

    // Section detail
    public string SectionName { get; set; } = null!;

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    // Mapping entities
    public LibraryZoneDto Zone { get; set; } = null!;

    [JsonIgnore]
    public ICollection<LibraryShelfDto> LibraryShelves { get; set; } = new List<LibraryShelfDto>();
}