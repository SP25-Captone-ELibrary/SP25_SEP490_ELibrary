using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class TrainedBookDetailDto
{
    public List<UntrainedGroup> TrainingData { get; set; } = new();

}
public class UntrainedGroup
{
    public List<ItemWithImagesForTraining> ItemsInGroup { get; set; } = new();
}

public class ItemWithImagesForTraining
{
    public int LibraryItemId { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<IFormFile> ImageFiles { get; set; } = new();
}
