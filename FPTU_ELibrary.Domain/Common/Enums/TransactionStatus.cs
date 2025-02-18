using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TransactionStatus
{
    [Description("Chưa thanh toán")]
    Pending,
    [Description("Đã thanh toán")]
    Paid,
    [Description("Đã hủy")]
    Cancelled
}