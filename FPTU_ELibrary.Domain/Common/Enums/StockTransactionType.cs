using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum StockTransactionType
{
    /// <summary>
    /// Completely new stock entry
    /// </summary>
    [Description("Nhập mới")]
    New,  

    /// <summary>
    /// Additional copies of an existing stock
    /// </summary>
    [Description("Nhập bổ sung")]
    Additional,  
    
    /// <summary>
    /// Items removed due to damage
    /// </summary>
    [Description("Rách, hư hỏng")]
    Damaged,  
    
    /// <summary>
    /// Items lost and written off
    /// </summary>
    [Description("Mất")]
    Lost,  
    
    /// <summary>
    /// Items are outdated
    /// </summary>
    [Description("Lỗi thời")]
    Outdated,

    /// <summary>
    /// Items transferred between locations
    /// </summary>
    [Description("Điều chuyển")]
    Transferred,
    
    /// <summary>
    /// Other cases
    /// </summary>
    [Description("Khác")]
    Other
}