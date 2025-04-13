using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Detection;

public class PredictResultDto
{
    public List<PredictionDto> Predictions { get; set; } = new();
}

public class PredictionDto
{
    public string TagName { get; set; } = null!;
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
    public ItemPredictedDetailDto BestItem { get; set; } = null!;
    public List<ItemPredictedDetailDto> OtherItems { get; set; } = new();
}

public class ItemPredictedDetailDto
{
    public MinimisedMatchResultDto OCRResult { get; set; } = null!;
    public int ObjectMatchResult { get; set; }
    public int LibraryItemId { get; set; }
}