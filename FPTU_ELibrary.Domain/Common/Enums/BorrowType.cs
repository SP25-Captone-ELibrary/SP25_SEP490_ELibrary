using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowType
{
    /// <summary>
    /// Only for reading inside the library
    /// </summary>
    [Description("Đọc tại chỗ")]
    InLibrary, 
    
    /// <summary>
    /// Borrowing away from the library 
    /// </summary>
    [Description("Mượn mang về")]
    TakeHome   
}