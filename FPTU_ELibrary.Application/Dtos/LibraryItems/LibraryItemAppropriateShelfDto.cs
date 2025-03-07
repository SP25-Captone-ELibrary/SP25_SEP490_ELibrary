using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemAppropriateShelfDto
{
    public string ItemClassificationNumber { get; set; } = null!;
    public LibraryFloorDto? Floor { get; set; }
    public LibraryZoneDto? Zone { get; set; }
    public LibrarySectionDto? Section { get; set; }
    public List<LibraryShelfDto> LibraryShelves { get; set; } = null!;
}

public static class LibraryItemAppropriateShelfDtoExtensions
{
    public static LibraryItemAppropriateShelfDto ToItemAppropriateShelfDto(
        this List<LibraryShelfDto> shelves, string itemClassificationNumber, LibrarySectionDto? section)
    {
        // Set null references
        if(section?.Zone.Floor != null) section.Zone.Floor.LibraryZones = new List<LibraryZoneDto>();
        if(section?.Zone != null) section.Zone.LibrarySections = new List<LibrarySectionDto>();
        if(section != null) section.LibraryShelves = new List<LibraryShelfDto>();
        
        return new()
        {
            ItemClassificationNumber = itemClassificationNumber,
            Floor = section?.Zone.Floor,
            Zone = section?.Zone,
            Section = section,
            LibraryShelves = shelves,
        };
    }
}