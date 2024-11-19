namespace FPTU_ELibrary.Domain.Entities;

public class LibraryFloor
{
    // Key
    public int FloorId { get; set; }
    
    // Floor number
    public string FloorNumber { get; set; } = null!;

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as deleted or not
    public bool IsDeleted { get; set; }

    // Mapping entity
    public ICollection<LibraryZone> LibraryZones { get; set; } = new List<LibraryZone>();
}
