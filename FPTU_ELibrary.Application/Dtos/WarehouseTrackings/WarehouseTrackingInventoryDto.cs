using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingInventoryDto
{
    // Key 
    public int TrackingId { get; set; }
    
    // Inventory management fields
    public int TotalItem { get; set; }
    public int TotalInstanceItem { get; set; }
    public int TotalCatalogedItem { get; set; }
    public int TotalCatalogedInstanceItem { get; set; }

    // Reference
    [JsonIgnore]
    public WarehouseTrackingDto WarehouseTracking { get; set; } = null!;
}