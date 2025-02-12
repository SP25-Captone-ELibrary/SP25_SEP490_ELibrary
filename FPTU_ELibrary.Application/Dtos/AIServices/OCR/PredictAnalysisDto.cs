namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class PredictAnalysisDto
{
    public List<OcrLineStatisticDto> LineStatisticDtos { get; set; }
    public List<StringComparision> StringComparisions { get; set; }
    public double MatchPercentage { get; set; }
    public double OverallPercentage { get; set; }
}
// right table in image
public class StringComparision
{
    public string MatchLine { get; set; }
    public double MatchPhrasePoint { get; set; }
    public double FuzzinessPoint { get; set; }
    public double  FieldThreshold { get; set; }
    public string PropertyName { get; set; }
    public double MatchPercentage { get; set; }

}

//left table in the figma image
public class OcrLineStatisticDto
{ 
    public string LineValue { get; set; }
    public int MatchedTitlePercentage { get; set; }
    public int MatchedAuthorPercentage { get; set; }
    public int MatchedPublisherPercentage { get; set; }
}