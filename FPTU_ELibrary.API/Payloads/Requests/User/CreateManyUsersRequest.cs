using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class CreateManyUsersRequest
{
    public IFormFile File { get; set; } = null!;
    public DuplicateHandle DuplicateHandle { get; set; }
    public bool IsSendEmail { get; set; }   
}