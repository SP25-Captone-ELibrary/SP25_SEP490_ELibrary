namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class TrainingImageMatchResultDto
{
    public List<SingleImageMatchResultDto> TrainingImageResult { get; set; }
}

public class SingleImageMatchResultDto : MatchResultDto
{
    public string ImageName { get; set; }
}