using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BookEditionCopyStatus
{
    [Description("Có sẵn trên kệ")]
    InShelf,
    [Description("Trong kho")]
    OutOfShelf,
    [Description("Đang được mượn")]
    Borrowed,
    [Description("Được đặt trước")]
    Reserved
}