using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum LibraryItemConditionStatus
{
    /// <summary>
    /// The item is in good condition
    /// </summary>
    [Description("Tốt")]
    Good,

    /// <summary>
    /// The item is worn or slightly damaged
    /// </summary>
    [Description("Bị rách")]
    Worn,

    /// <summary>
    /// The item is heavily damaged and likely unusable
    /// </summary>
    [Description("Bị hỏng nặng")]
    Damaged,

    /// <summary>
    /// The item is lost and cannot be located.
    /// </summary>
    [Description("Mất")]
    Lost
}