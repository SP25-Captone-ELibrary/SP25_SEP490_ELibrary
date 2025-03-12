using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowType
{
    /// <summary>
    /// Only for reading inside the library
    /// </summary>
    [Description("Mượn tài quầy")]
    InLibrary, 
    
    /// <summary>
    /// Borrowing away from the library 
    /// </summary>
    [Description("Mượn từ xa")]
    TakeHome   
}