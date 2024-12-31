using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowRequestStatus
{
    [Description("Đang chờ duyệt")]
    Pending,
    [Description("Yêu cầu được chấp nhận")]
    Approved,
    [Description("Yêu cầu bị từ chối")]
    Rejected,
    [Description("Yêu cầu bị hủy")]
    Cancelled
}