using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibrarySection
{
    // Key
    public int SectionId { get; set; }
    
    // Zone belongs to
    public int ZoneId { get; set; }
    
    // Section detail
    public string EngSectionName { get; set; } = null!;
    public string VieSectionName { get; set; } = null!;
    public string ShelfPrefix { get; set; } = null!;
    
    // DDC Range
    public decimal ClassificationNumberRangeFrom { get; set; }
    public decimal ClassificationNumberRangeTo { get; set; }
    
    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }
    
    // Mark as children section
    public bool IsChildrenSection { get; set; }
    
    // Mark as reference section
    public bool IsReferenceSection { get; set; }
    
    // Mark as journal section
    public bool IsJournalSection { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public LibraryZone Zone { get; set; } = null!;

    public ICollection<LibraryShelf> LibraryShelves { get; set; } = new List<LibraryShelf>();
}
