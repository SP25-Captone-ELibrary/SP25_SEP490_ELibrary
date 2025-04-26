namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class UpdateShelfRequest
{
    public string ShelfNumber { get; set; } = null!;
    public string? EngShelfName { get; set; }
    public string? VieShelfName { get; set; }
    public decimal ClassificationNumberRangeFrom { get; set; }
    public decimal ClassificationNumberRangeTo { get; set; }
}