using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BookFormat
{
    [Description("Sách bìa giấy")]
    Paperback,
    [Description("Sách bìa cứng")]
    HardCover
}