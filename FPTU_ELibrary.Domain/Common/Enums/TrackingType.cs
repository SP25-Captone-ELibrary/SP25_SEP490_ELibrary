using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TrackingType
{
    [Description("Nhập kho")]
    StockIn,
    [Description("Xuất kho")]
    StockOut,
    [Description("Trao đổi")]
    Transfer
}