namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class ClassificationComponentsDto
{
    public List<PredictionDto> Predictions { get; set; } = new();
}

public class PredictionDto
{
    public string TagName { get; set; } = null!;
    public double Probability { get; set; }
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class IterationDto
{
    public string Status { get; set; } = null!;
    public Guid Id { get; set; }
    public string? PublishName { get;set; }
}
