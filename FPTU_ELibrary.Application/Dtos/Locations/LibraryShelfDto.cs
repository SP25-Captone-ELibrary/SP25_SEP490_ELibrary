using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryShelfDto
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
    // public ICollection<BookEditionCopyDto> BookEditionCopies { get; set; } = new List<BookEditionCopyDto>();
    public ICollection<BookEditionDto> BookEditions { get; set; } = new List<BookEditionDto>();

    [JsonIgnore]
    public ICollection<LearningMaterialDto> LearningMaterials { get; set; } = new List<LearningMaterialDto>();

    public LibrarySectionDto Section { get; set; } = null!;
}