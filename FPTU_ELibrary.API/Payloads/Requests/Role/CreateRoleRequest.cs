using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Role;

public class CreateRoleRequest
{
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public RoleType RoleTypeIdx { get; set; }
}