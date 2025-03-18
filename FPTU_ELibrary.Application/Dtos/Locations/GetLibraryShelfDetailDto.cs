using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class GetLibraryShelfDetailDto
{
    public LibraryFloorDto? Floor { get; set; } = null!;
    public LibraryZoneDto? Zone { get; set; } = null!;
    public LibrarySectionDto? Section { get; set; } = null!;
    public LibraryShelfDto LibraryShelf { get; set; } = null!;
}

public static class GetLibraryShelfDetailDtoExtensions
{
    public static GetLibraryShelfDetailDto ToGetLibraryShelfDetailDto(
        this LibraryShelfDto shelf)
    {
        if (shelf.Section != null!)
        {
            // Set null references
            if(shelf.Section?.Zone.Floor != null) shelf.Section.Zone.Floor.LibraryZones = new List<LibraryZoneDto>();
            if(shelf.Section?.Zone != null) shelf.Section.Zone.LibrarySections = new List<LibrarySectionDto>();
            if(shelf.Section != null) shelf.Section.LibraryShelves = new List<LibraryShelfDto>();
        }      
        
        return new()
        {
            Floor = shelf.Section?.Zone.Floor,
            Zone = shelf.Section?.Zone,
            Section = shelf.Section,
            LibraryShelf = shelf,
        };
    }
}