namespace FPTU_ELibrary.Domain.Common.Enums;

public enum ReservationQueueStatus
{
    /// <summary>
    /// Waiting for availability
    /// </summary>
    Pending,     
    
    /// <summary>
    /// Item is available and assigned
    /// </summary>
    Assigned,    
    
    /// <summary>
    /// User collected the item
    /// </summary>
    Collected,   
    
    /// <summary>
    /// Reservation expired due to non-collection
    /// </summary>
    Expired,     
    
    /// <summary>
    /// User/staff cancelled 
    /// </summary>
    Cancelled    
}