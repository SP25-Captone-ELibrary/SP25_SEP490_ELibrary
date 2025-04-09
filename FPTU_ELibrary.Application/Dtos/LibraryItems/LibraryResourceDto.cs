using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Payments;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryResourceDto
{
    // Key
    public int ResourceId { get; set; }

    // Resource detail information
    public string ResourceTitle { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public string FileFormat { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string? ProviderMetadata { get; set; }
    public bool IsDeleted { get; set; }
    public int DefaultBorrowDurationDays { get; set; }
    public decimal BorrowPrice { get; set; }
    public string? S3OriginalName { get; set; }
    // Creation, update datetime and employee who in charge of creation
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    [JsonIgnore]
    public ICollection<LibraryItemResourceDto> LibraryItemResources { get; set; } = new List<LibraryItemResourceDto>();
    
    [JsonIgnore]
    public ICollection<DigitalBorrowDto> DigitalBorrows { get; set; } = new List<DigitalBorrowDto>();
    
    [JsonIgnore] 
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    
    [JsonIgnore] 
    public ICollection<LibraryResourceUrlDto> LibraryResourceUrls { get; set; } = new List<LibraryResourceUrlDto>();
    
    [JsonIgnore]
    public ICollection<BorrowRequestResourceDto> BorrowRequestResources { get; set; } = new List<BorrowRequestResourceDto>();
}