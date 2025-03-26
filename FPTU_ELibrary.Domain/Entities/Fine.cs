using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class Fine
{
    // Key
    public int FineId { get; set; }

    // Fine for which borrow record detail
    public int BorrowRecordDetailId { get; set; }

    // Specific fine
    public int FinePolicyId { get; set; }
    
    // Fine detail
    public decimal FineAmount { get; set; } // Amount from fine policy, but required to input when amount in policy equals to 0 
    public string? FineNote { get; set; }
    public FineStatus Status { get; set; }
    
    // Datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryAt { get; set; }
    
    // Created by 
    public Guid CreatedBy { get; set; }

    // Mapping entities
    [JsonIgnore]
    public BorrowRecordDetail BorrowRecordDetail { get; set; } = null!;
    public Employee CreateByNavigation { get; set; } = null!;
    public FinePolicy FinePolicy { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
