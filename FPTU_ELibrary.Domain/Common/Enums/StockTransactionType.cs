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
    /// Request to reorder additional copies for a specific item already in the stock list.
    /// This is used when the item is in high demand, prompting a formal request to order more copies.
    /// </summary>
    [Description("Yêu cầu nhập bổ sung")]
    Reorder,
    
    /// <summary>
    /// Other cases
    /// </summary>
    [Description("Khác")]
    Other
}