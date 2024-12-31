namespace FPTU_ELibrary.Application.Configurations;

//	Summary:
//		Configure system configuration elements
public class AISettings
{
    public string SubscriptionKey { get; set; }
    public string Endpoint { get; set; }
    public double TitlePercentage { get; set; }
    public double AuthorNamePercentage { get; set; }
    public double PublisherPercentage { get; set; }
    public int ConfidenceThreshold { get; set; }
}
