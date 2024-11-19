using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRequest
{
    // Key
    public int BorrowRequestId { get; set; }

    // Request for which edition
    public int? BookEditionId { get; set; }

    // Request for particular copy 
    public int? BookEditionCopyId { get; set; }

    // Request for learning material
    public int? LearningMaterialId { get; set; }

    // Who make request
    public Guid UserId { get; set; }

    // Create and expiration datetime
    public DateTime RequestDate { get; set; }
    public DateTime ExpirationDate { get; set; }

    // Request detail and status
    public string Status { get; set; } = null!;
    public string BorrowType { get; set; } = null!;
    public string? Description { get; set; }

    // Deposit fee for request remotely
    public decimal? DepositFee { get; set; }
    public bool DepositPaid { get; set; }

    //public Guid? ProcessedBy { get; set; }
    //public DateTime? ProcessedDate { get; set; }

    // Mapping entities
    public BookEdition? BookEdition { get; set; }
    public BookEditionCopy? BookEditionCopy { get; set; }
    public LearningMaterial? LearningMaterial { get; set; }
    //public Employee? ProcessedByNavigation { get; set; }
    public User User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
