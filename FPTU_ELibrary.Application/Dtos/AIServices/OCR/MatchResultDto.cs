namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class MatchResultDto
{
        public List<FieldMatchedResult>  FieldPointsWithThreshole { get; set; }
        public double TotalPoint { get; set; }
        public double ConfidenceThreshold { get; set; }
        public string ImageName { get; set; }
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