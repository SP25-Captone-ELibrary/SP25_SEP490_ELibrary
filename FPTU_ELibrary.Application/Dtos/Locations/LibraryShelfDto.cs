using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryShelfDto
{
    // Key
    public int ShelfId { get; set; }
    
    // Section belongs to 
    public int SectionId { get; set; }
    
    // Shelf detail
    public string ShelfNumber { get; set; } = null!;
    public string? EngShelfName { get; set; }
    public string? VieShelfName { get; set; }
    
    // DDC Range
    public decimal ClassificationNumberRangeFrom { get; set; }
    public decimal ClassificationNumberRangeTo { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    [JsonIgnore]
    public LibrarySectionDto Section { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<LibraryItemDto> LibraryItems { get; set; } = new List<LibraryItemDto>();
}