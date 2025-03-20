using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum FineStatus
{
    [Description("Chưa thanh toán")]
    Pending,
    [Description("Đã thanh toán")]
    Paid,
    [Description("Đã hết hạn")]
    Expired
}