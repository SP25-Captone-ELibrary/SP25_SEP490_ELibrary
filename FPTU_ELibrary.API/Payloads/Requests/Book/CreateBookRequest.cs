namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookRequest
{
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; } = null!;
    public string? Summary { get; set; }
    public List<CreateBookEditionRequest> BookEditions { get; set; } = new();
    public List<int> CategoryIds { get; set; } = new();
}