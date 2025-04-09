using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TrackingType
{
    [Description("Nhập kho")]
    StockIn,
    [Description("Yêu cầu bổ sung")]
    SupplementRequest,
    [Description("Kiểm kê")]
    StockChecking,
    [Description("Xuất kho")]
    StockOut
}