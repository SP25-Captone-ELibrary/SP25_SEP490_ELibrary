using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowRequestStatus
{
    /// <summary>
    /// The request is created and waiting for the user to pick up the item
    /// </summary>
    [Description("Chờ nhận")]
    Created,
    
    /// <summary>
    /// The user didn't pick up the item before ExpirationDate
    /// </summary>
    [Description("Hết hạn")]
    Expired,
    
    /// <summary>
    /// The user picked up the item, and a BorrowRecord has been created
    /// </summary>
    [Description("Đã mượn")]
    Borrowed, 
    
    /// <summary>
    /// The user cancels the request 
    /// </summary>
    [Description("Đã hủy")]
    Cancelled 
}