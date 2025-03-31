using FPTU_ELibrary.Domain.Common.Enums;
using TransactionStatus = FPTU_ELibrary.Domain.Common.Enums.TransactionStatus;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class TransactionSpecParams : BaseSpecParams
{
    public TransactionStatus? TransactionStatus { get; set; }
    public TransactionType? TransactionType { get; set; }
    public TransactionMethod? TransactionMethod { get; set; }
    
    public DateTime?[]? TransactionDateRange { get; set; }
    public DateTime?[]? ExpiredAtRange { get; set; }
    public DateTime?[]? CancelledAtRange { get; set; }
    public decimal?[]? AmountRange { get; set; }
}