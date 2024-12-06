namespace FPTU_ELibrary.Domain.Common.Enums;

public enum PermissionLevel
{
    /// <summary>
    /// Not allow to access
    /// </summary>
    AccessDenied = 0,
    /// <summary>
    /// View only 
    /// </summary>
    View = 1,
    /// <summary>
    /// Can view and modify
    /// </summary>
    Modify = 2,
    /// <summary>
    /// Can view, modify and create
    /// </summary>
    Create = 3,
    /// <summary>
    /// No limit access
    /// </summary>
    FullAccess = 4
}