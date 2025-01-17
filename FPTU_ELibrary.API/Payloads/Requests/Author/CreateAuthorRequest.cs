namespace FPTU_ELibrary.API.Payloads.Requests.Author;

public class CreateAuthorRequest
{
    public string AuthorCode { get; set; } = null!;
    public string? AuthorImage { get; set; }
    public string FullName { get; set; } = null!;
    public string? Biography { get; set; } // Save as HTML text
    public DateTime? Dob { get; set; }
    public DateTime? DateOfDeath { get; set; } 
    public string? Nationality { get; set; } 
}
