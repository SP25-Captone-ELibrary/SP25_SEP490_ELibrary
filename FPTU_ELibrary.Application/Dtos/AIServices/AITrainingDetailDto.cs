using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class AITrainingDetailDto
{
    // Key
    public int TrainingDetailId { get; set; }
    
    // In which session
    public int TrainingSessionId { get; set; }
    
    // For which library item
    public int LibraryItemId { get; set; }
    
    // References
    [JsonIgnore]
    public AITrainingSessionDto TrainingSession { get; set; } = null!;
    public LibraryItemDto LibraryItem { get; set; } = null!;
    
    // Navigations
    public ICollection<AITrainingImageDto> TrainingImages { get; set; } = new List<AITrainingImageDto>();
}