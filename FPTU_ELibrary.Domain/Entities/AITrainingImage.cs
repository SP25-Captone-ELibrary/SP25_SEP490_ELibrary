using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class AITrainingImage
{
    // Key
    public int TrainingImageId { get; set; }
    
    // For which training detail
    public int TrainingDetailId { get; set; }
    
    // Image url 
    public string ImageUrl { get; set; } = null!;

    // References
    [JsonIgnore]
    public AITrainingDetail TrainingDetail { get; set; } = null!;
}