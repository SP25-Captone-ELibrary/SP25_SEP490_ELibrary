namespace FPTU_ELibrary.Application.Configurations;

public class CustomVisionSettings
{
    public string PredictionKey { get; set; }
    public string PredictionEndpoint { get; set; }
    public string TrainingKey { get; set; }
    public string TrainingEndpoint { get; set; }
    public string SubscriptionKey { get; set; }
    public string ProjectId { get; set; }
    public string ResourceGroup { get; set; }
    public string Provider { get; set; }
    public string Account { get; set; }
    public string? PublishedName { get; set; }
    public string BaseAIUrl { get; set; }
}