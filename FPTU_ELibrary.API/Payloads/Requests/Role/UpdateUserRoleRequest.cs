namespace FPTU_ELibrary.API.Payloads.Requests.Role;

public class UpdateUserRoleRequest
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
}