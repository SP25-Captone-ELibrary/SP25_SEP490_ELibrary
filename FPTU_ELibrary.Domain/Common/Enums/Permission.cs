using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum Permission
{
    [Description("Toàn quyền truy cập")]
    FullAccess,
    [Description("Xem")]
    View,
    [Description("Tạo mới")]
    Create,
    [Description("Chỉnh sửa")]
    Modify,
    [Description("Hạn chế")]
    AccessDenied
}