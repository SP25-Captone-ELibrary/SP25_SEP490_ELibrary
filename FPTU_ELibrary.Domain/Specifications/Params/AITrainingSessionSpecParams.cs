using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class AITrainingSessionSpecParams : BaseSpecParams
{
    public AITrainingStatus? TrainingStatus { get; set; }
    public DateTime?[]? TrainDateRange { get; set; }
}