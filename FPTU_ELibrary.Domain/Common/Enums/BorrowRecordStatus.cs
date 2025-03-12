using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowRecordStatus
{
    [Description("Đang mượn")]
    Borrowing,

    [Description("Đã trả")]
    Returned,

    [Description("Quá hạn")]
    Overdue,

    [Description("Mất")]
    Lost
}