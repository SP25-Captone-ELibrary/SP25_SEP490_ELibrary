using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class AITrainingSession
{
    // Key
    public int TrainingSessionId { get; set; }
    
    // Training audit information
    public AIModel Model { get; set; }
    public int TotalTrainedItem { get; set; }
    public decimal? TotalTrainedTime { get; set; }
    public AITrainingStatus TrainingStatus { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Train date & by
    public DateTime TrainDate { get; set; }
    public string TrainBy { get; set; } = null!;
    
    // Navigations
    public ICollection<AITrainingDetail> TrainingDetails { get; set; } = new List<AITrainingDetail>();
}