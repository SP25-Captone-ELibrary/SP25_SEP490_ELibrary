using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Payments;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FineDto
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
    public BorrowRecordDto BorrowRecord { get; set; } = null!;
    public EmployeeDto CreateByNavigation { get; set; } = null!;
    public FinePolicyDto FinePolicy { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}