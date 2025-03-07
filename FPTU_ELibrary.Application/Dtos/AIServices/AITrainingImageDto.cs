using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class AITrainingImageDto
{
    // Key
    public int TrainingImageId { get; set; }
    
    // For which training detail
    public int TrainingDetailId { get; set; }
    
    // Image url 
    public string ImageUrl { get; set; } = null!;

    // References
    [JsonIgnore]
    public AITrainingDetailDto TrainingDetail { get; set; } = null!;
}