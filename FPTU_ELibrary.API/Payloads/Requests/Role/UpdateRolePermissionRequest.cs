namespace FPTU_ELibrary.API.Payloads.Requests.Role;

public class UpdateRolePermissionRequest
{
    public int ColId { get; set; }
    public int RowId { get; set; }
    public int PermissionId { get; set; }
    public bool IsRoleVerticalLayout { get; set; } 
}