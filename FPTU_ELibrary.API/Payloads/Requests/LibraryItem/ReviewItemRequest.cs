namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class ReviewItemRequest
{
    public int LibraryItemId { get; set; }
    public double RatingValue { get; set; }
    public string? ReviewText { get; set; }
}