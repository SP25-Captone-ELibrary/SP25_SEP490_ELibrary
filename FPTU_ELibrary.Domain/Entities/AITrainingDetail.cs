using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class AITrainingDetail
{
    // Key
    public int TrainingDetailId { get; set; }
    
    // In which session
    public int TrainingSessionId { get; set; }
    
    // For which library item
    public int LibraryItemId { get; set; }
    
    // References
    [JsonIgnore]
    public AITrainingSession TrainingSession { get; set; } = null!;
    public LibraryItem LibraryItem { get; set; } = null!;
    
    // Navigations
    public ICollection<AITrainingImage> TrainingImages { get; set; } = new List<AITrainingImage>();
}