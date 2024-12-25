namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class UpdateBookRequest
{
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Summary { get; set; }
    public List<int> CategoryIds { get; set; } = new();
}