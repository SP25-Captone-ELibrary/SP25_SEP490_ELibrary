using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryShelf
{
    // Key
    public int ShelfId { get; set; }
    
    // Section belongs to 
    public int SectionId { get; set; }
    
    // Shelf detail
    public string ShelfNumber { get; set; } = null!;

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }

    [JsonIgnore]
    public ICollection<BookEditionCopy> BookEditionCopies { get; set; } = new List<BookEditionCopy>();

    [JsonIgnore]
    public ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();

    public LibrarySection Section { get; set; } = null!;
}
