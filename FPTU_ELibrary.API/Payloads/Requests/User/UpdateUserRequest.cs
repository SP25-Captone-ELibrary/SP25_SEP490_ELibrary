namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class UpdateUserRequest
{
    // Student detail and credentials information
    public string? FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public string? Phone { get; set; }
    //Field use to update role for account
    public string? UserCode { get; set; }
    public int? RoleId { get; set; }

    //Recognise who update account
    public string ModifyBy { get; set; }

    // public string? Avatar { get; set; } 

    
}