namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class ItemGroupForAIDto
{
    public Guid TrainingCode { get; set; }
    public List<int> NewLibraryIdsToTrain { get; set; }     
}