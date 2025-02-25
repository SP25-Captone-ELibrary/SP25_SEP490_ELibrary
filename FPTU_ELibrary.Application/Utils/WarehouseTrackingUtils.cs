using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Utils;

public static class WarehouseTrackingUtils
{
    public static readonly Dictionary<TrackingType, List<StockTransactionType>> TransactionTypeRelations = new()
    {
        { TrackingType.StockIn , new() { StockTransactionType.New, StockTransactionType.Additional, StockTransactionType.Other} },
        { TrackingType.StockOut , new() { StockTransactionType.Damaged, StockTransactionType.Lost, StockTransactionType.Outdated, StockTransactionType.Other } },
        { TrackingType.Transfer , new() { StockTransactionType.Transferred , StockTransactionType.Other}}
    };
}