using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowDigitalStatus
{
    [Description("Đang mượn")]
    Active,
    [Description("Hết hạn")]
    Expired,
    [Description("Đã hủy")]
    Cancelled
}