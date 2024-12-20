using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class SearchAndFilterUserRequest : BaseSpecParams
{
    public MultipleFieldSearch MultipleFieldSearch { get; set; } = null!;
}

public record MultipleFieldSearch
{
    public string? UserCode { get; set; }
    public string? Email { get; set; } = null!;
    public string? FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public bool? IsActive { get; set; }
}

public static class SearchAndFilterUserRequestExtension
{
    public static UserDto ToUserDto(this SearchAndFilterUserRequest req)
    {
        return new UserDto()
        {
            UserCode = req.MultipleFieldSearch.UserCode,
            Email = req.MultipleFieldSearch.Email?? "",
            FirstName = req.MultipleFieldSearch.FirstName,
            LastName = req.MultipleFieldSearch.LastName,
            Phone= req.MultipleFieldSearch.Phone,
            IsActive = req.MultipleFieldSearch.IsActive?? true,
            
        };
    }
}