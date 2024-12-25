namespace FPTU_ELibrary.Domain.Common.Enums;

public enum TrailType
{
    /// <summary>
    /// Entity was not changed
    /// </summary>
    None = 0,

    /// <summary>
    /// Entity was added
    /// </summary>
    Added = 1,

    /// <summary>
    /// Entity was modified
    /// </summary>
    Modified = 2,

    /// <summary>
    /// Entity was deleted
    /// </summary>
    Deleted = 3
}