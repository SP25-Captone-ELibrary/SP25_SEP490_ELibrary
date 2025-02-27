using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum NotificationType
{
    [Description("Sự kiện")]
    Event,
    [Description("Thông báo trả sách")]
    Reminder,
    [Description("Thông báo chung")]
    Notice
}