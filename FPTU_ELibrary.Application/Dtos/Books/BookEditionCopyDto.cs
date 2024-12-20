using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class BookEditionCopyDto
{
    // Key
    public int BookEditionCopyId { get; set; }

    // Copy of which edition
    public int BookEditionId { get; set; }
    
    // Locate in which shelf
    public int? ShelfId { get; set; }

    // Copy code and its status
    public string? Code { get; set; }
    public string Status { get; set; } = null!;
    
    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete
    public bool IsDeleted { get; set; }

    // Mapping entities
    public LibraryShelfDto? Shelf { get; set; }

    [JsonIgnore]
    public BookEditionDto BookEdition { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecordDto> BorrowRecords { get; set; } = new List<BorrowRecordDto>();

    [JsonIgnore]
    public ICollection<BorrowRequestDto> BorrowRequests { get; set; } = new List<BorrowRequestDto>();

    public ICollection<CopyConditionHistoryDto> CopyConditionHistories { get; set; } = new List<CopyConditionHistoryDto>();
}