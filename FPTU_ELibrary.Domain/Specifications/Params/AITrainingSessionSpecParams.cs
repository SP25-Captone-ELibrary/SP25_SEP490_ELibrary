namespace FPTU_ELibrary.Domain.Specifications.Params;

public class AITrainingSessionSpecParams : BaseSpecParams
{
    public string? TrainingStatus { get; set; }
    public DateTime?[]? TrainDateRange { get; set; }
}