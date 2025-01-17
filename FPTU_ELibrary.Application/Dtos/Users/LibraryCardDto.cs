using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Users;

public class LibraryCardDto
{
    // Key
    public Guid LibraryCardId { get; set; }
    
    // Library card information
    public string FullName { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    
    // Issue method: in-person | online & request status
    public string IssuanceMethod { get; set; } = null!;
    public string RequestStatus { get; set; } = null!;

    // Issue and expiry date
    public bool IsActive { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<UserDto> Users { get; set; } = null!;
}