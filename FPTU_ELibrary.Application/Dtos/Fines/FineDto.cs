using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FineDto
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
    public BorrowRecordDetailDto BorrowRecordDetail { get; set; } = null!;
    public EmployeeDto CreateByNavigation { get; set; } = null!;
    public FinePolicyDto FinePolicy { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}