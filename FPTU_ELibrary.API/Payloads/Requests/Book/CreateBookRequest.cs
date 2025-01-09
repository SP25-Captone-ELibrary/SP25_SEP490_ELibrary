namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookRequest
{
    public string BookCode { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; } = null!;
    public string? Summary { get; set; }
    // Book editions
    public List<CreateBookEditionRequest> BookEditions { get; set; } = new();
    // Book resources
    public List<CreateBookResourceRequest>? BookResources { get; set; } = new();
    public List<int> CategoryIds { get; set; } = new();
}

