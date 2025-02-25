using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateStockInDetailRequest
{
    public string ItemName { get; set; } = null!;
    public int ItemTotal { get; set; }
    public string? Isbn { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public int ConditionId { get; set; }
    public int CategoryId { get; set; }
    public StockTransactionType StockTransactionType { get; set; }
    public int? LibraryItemId { get; set; }
    public CreateStockInDetailItemRequest? LibraryItem { get; set; }
}
