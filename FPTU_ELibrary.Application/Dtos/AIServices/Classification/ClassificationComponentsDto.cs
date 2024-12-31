namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class ClassificationComponentsDto
{
    public List<PredictionDto> Predictions { get; set; }
}

public class PredictionDto
{
    public string TagName { get; set; }
    public double Probability { get; set; }
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class IterationDto
{
    public string Status { get; set; }
    public Guid Id { get; set; }
    public string? PublishName { get;set; }
}
