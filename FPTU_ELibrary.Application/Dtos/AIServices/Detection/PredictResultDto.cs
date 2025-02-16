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

// public class PredictionResponseDto
// {
//     public int NumberOfBookDetected { get; set; }
//     public List<PossibleLibraryItem> LibraryItemPrediction { get; set; }
// }
//
// public class PossibleLibraryItem
// {
//     public string BookCode { get; set; }
//     public List<LibraryItemDto> LibraryItemDetails { get; set; }
// }

public class PredictionResponseDto
{
    public ItemPredictedDetailDto BestItem { get; set; }
    public List<ItemPredictedDetailDto> OtherItems { get; set; }
}

public class ItemPredictedDetailDto
{
    public MinimisedMatchResultDto OCRResult { get; set; }
    public int ObjectMatchResult { get; set; }
    public int LibraryItemId { get; set; }
}