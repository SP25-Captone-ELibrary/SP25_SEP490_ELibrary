namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class MatchResultDto : BaseMatchResultDto
{
    public List<FieldMatchedResult> FieldPointsWithThreshole { get; set; }
}
public class BaseMatchResultDto
{
    public double TotalPoint { get; set; }
    public double ConfidenceThreshold { get; set; }
    public string ImageName { get; set; }
}
public class MinimisedMatchResultDto : BaseMatchResultDto
{
    public List<MinimisedFieldMatchedResult> FieldPointsWithThreshole { get; set; }
}

public class FieldMatchedResult
{
    public string Name { get; set; }
    public string Detail { get; set; }
    public int FuzzinessPoint { get; set; }
    public int MatchPhrasePoint { get; set; }
    public int MatchedPoint { get; set; }
    public double Threshold { get; set; }
    public bool IsPassed { get; set; }
}

public class MinimisedFieldMatchedResult
{
    public string Name { get; set; }
    public int MatchedPoint { get; set; }
    public double Threshold { get; set; }
    public bool IsPassed { get; set; }
}

public static class FieldMatchedResultExtension
{
    public static MinimisedFieldMatchedResult ToMinimisedFieldMatchedResult(this FieldMatchedResult fieldMatchedResult)
    {
        var response = new MinimisedFieldMatchedResult
        {
            MatchedPoint = fieldMatchedResult.MatchedPoint,
            Threshold = fieldMatchedResult.Threshold,
            IsPassed = fieldMatchedResult.IsPassed
        };
        if (fieldMatchedResult.Name.Equals("Title or Subtitle matches most"))
        {
            response.Name = "Title";
        }
        else
            response.Name = fieldMatchedResult.Name; 

        return response;
    }
    public static MinimisedMatchResultDto ToMinimisedMatchResultDto(this MatchResultDto matchResultDto)
    {
        var response = new MinimisedMatchResultDto
        {
            TotalPoint = matchResultDto.TotalPoint,
            ConfidenceThreshold = matchResultDto.ConfidenceThreshold,
            ImageName = matchResultDto.ImageName,
            FieldPointsWithThreshole = matchResultDto.FieldPointsWithThreshole.Select(x => x.ToMinimisedFieldMatchedResult()).ToList()
        };
        return response;
    }
}