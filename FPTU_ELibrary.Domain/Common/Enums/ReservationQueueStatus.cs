using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum ReservationQueueStatus
{
    /// <summary>
    /// Waiting for availability
    /// </summary>
    [Description("Đang chờ")]  
    Pending,     
    
    /// <summary>
    /// Item is available and assigned
    /// </summary>
    [Description("Đã được gán")]  
    Assigned,    
    
    /// <summary>
    /// User collected the item
    /// </summary>
    [Description("Đã lấy")]
    Collected,
    
    /// <summary>
    /// Reservation expired due to non-collection
    /// </summary>
    [Description("Đã hết hạn")]
    Expired,     
    
    /// <summary>
    /// User/staff cancelled 
    /// </summary>
    [Description("Đã hủy")]
    Cancelled    
}