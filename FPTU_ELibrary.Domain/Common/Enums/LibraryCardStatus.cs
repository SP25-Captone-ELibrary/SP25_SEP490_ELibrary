using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum LibraryCardStatus
{
    [Description("Chưa thanh toán")]
    UnPaid, // Register but not yet paid
    [Description("Đang chờ duyệt")]
    Pending, // Register but not yet confirmed
    [Description("Đang hoạt động")]
    Active, // Card has been confirmed, card is now active
    [Description("Bị từ chối")]
    Rejected, // Card has been rejected 
    [Description("Hết hạn")]
    Expired, // Card expired
    [Description("Bị cấm")]
    Suspended, // Disabled the card due to rule violations or deactivated by admin or librarian
}