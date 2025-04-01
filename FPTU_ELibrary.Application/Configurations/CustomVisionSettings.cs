namespace FPTU_ELibrary.Application.Configurations;

public class CustomVisionSettings
{
    public string PredictionKey { get; set; } = null!;
    public string PredictionEndpoint { get; set; } = null!;
    public string TrainingKey { get; set; } = null!;
    public string TrainingEndpoint { get; set; } = null!;
    public string SubscriptionKey { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string ResourceGroup { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Account { get; set; } = null!;
    public string PublishedName { get; set; } = null!;
    public string BaseAIUrl { get; set; } = null!;
    public string BasePredictUrl { get; set; } = null!;
    public int AvailableGroupToTrain { get; set; }
}