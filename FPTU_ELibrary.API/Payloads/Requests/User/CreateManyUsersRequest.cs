using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class CreateManyUsersRequest
{
    public IFormFile File { get; set; } = null!;
    public string DuplicateHandle { get; set; }
}