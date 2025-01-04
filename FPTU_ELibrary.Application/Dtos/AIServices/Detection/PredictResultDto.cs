using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Detection;

public class PredictResultDto
{
    public List<PredictionDto> Predictions { get; set; }
}

public class PredictionDto
{
    public string TagName { get; set; }
    public double Probability { get; set; }
}

public class PredictionResponseDto
{
    public int NumberOfBookDetected { get; set; }
    public List<PossibleBookEdition> BookEditionPrediction { get; set; }
}

public class PossibleBookEdition
{
    public string BookCode { get; set; }
    public List<BookEditionDetailDto> BookEditionDetails { get; set; }
}