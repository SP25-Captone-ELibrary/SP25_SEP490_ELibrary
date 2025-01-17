using FPTU_ELibrary.Application.Dtos.LibraryItems;

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
    public List<PossibleLibraryItem> LibraryItemPrediction { get; set; }
}

public class PossibleLibraryItem
{
    public string BookCode { get; set; }
    public List<LibraryItemDto> LibraryItemDetails { get; set; }
}