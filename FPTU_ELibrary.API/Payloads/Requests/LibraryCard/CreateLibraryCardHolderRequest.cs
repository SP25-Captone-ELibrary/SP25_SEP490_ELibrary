namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class CreateLibraryCardHolderRequest
{
    // User information
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Gender { get; set; } = null!;
    public DateTime? Dob { get; set; }
    
    // Library card information
    public string Avatar { get; set; } = null!;
}