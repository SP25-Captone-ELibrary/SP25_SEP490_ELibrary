namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowType
{
    /// <summary>
    /// Only for reading inside the library
    /// </summary>
    InLibrary, 
    
    /// <summary>
    /// Borrowing away from the library 
    /// </summary>
    TakeHome   
}