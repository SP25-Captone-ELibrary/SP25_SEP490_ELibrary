namespace FPTU_ELibrary.Application.Dtos.AdminConfiguration;

public class AISettingsDto
{
    public double AuthorNamePercentage { get; set; }
    public double TitlePercentage { get; set; }
    public double PublisherPercentage { get; set; }
    public int? ConfidenceThreshold { get; set; }
    public int? MinFieldThreshold { get; set; }  
}