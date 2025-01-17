using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class Fine
{
    // Key
    public int FineId { get; set; }

    // Fine for which borrow record
    public int BorrowRecordId { get; set; }

    // Specific fine
    public int FinePolicyId { get; set; }
    
    // Fine detail
    public string? FineNote { get; set; }
    public string Status { get; set; } = null!;
    
    // Datetime
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryAt { get; set; }
    
    // Created by 
    public Guid CreatedBy { get; set; }

    // Mapping entities
    public BorrowRecord BorrowRecord { get; set; } = null!;
    public Employee CreateByNavigation { get; set; } = null!;
    public FinePolicy FinePolicy { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
