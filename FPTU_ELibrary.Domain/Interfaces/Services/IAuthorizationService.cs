namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuthorizationService
{
    Task<bool> IsAuthorizedAsync(string role, string feature, string httpMethod);
}