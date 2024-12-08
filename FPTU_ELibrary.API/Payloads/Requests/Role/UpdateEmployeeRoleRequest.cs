namespace FPTU_ELibrary.API.Payloads.Requests.Role;

public class UpdateEmployeeRoleRequest
{
    public Guid EmployeeId { get; set; }
    public int RoleId { get; set; }
}