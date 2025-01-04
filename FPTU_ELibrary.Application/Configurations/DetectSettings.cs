namespace FPTU_ELibrary.Application.Configurations;

public class DetectSettings
{
    public string DetectAPIUrl { get; set; }
    public string DetectAPIKey { get; set; }
    public string DetectModelUrl { get; set; }
    public int DetectImageSize { get; set; }
    public double DetectConfidence { get; set; }
    public double DetectIOU { get; set; }
}