using System.Transactions;
using FPTU_ELibrary.Domain.Common.Enums;
using TransactionStatus = FPTU_ELibrary.Domain.Common.Enums.TransactionStatus;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class TransactionSpecParams :BaseSpecParams
{
    public TransactionStatus? TransactionStatus { get; set; }
    public TransactionType? TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime?[]? CreatedAtRange { get; set; }
    public DateTime?[]? CancelledAtRange { get; set; }
    public decimal?[]? AmountRange { get; set; }
}