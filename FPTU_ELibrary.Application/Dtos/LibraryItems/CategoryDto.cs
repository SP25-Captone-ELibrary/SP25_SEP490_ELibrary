using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class CategoryDto
{
    // Key
    public int CategoryId { get; set; }
    
    // Prefix: SGK, STK, SK
    public string Prefix { get; set; } = null!;

    // Category detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }
    
    // Boolean value to determine whether item need to create with image or not 
    // This field really essential as it would allow items to train data into AI storage
    public bool IsAllowAITraining { get; set; }

    // Mapping entities
    [JsonIgnore] 
    public ICollection<LibraryItemDto> LibraryItems { get; set; } = new List<LibraryItemDto>();
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetailDto> WarehouseTrackingDetails { get; set; } = new List<WarehouseTrackingDetailDto>();
}