namespace FPTU_ELibrary.Application.Dtos.Roles;

public class UserPermissionCellDto
{
    public int ColId { get; set; }
    public int RowId { get; set; }
    public int PermissionId { get; set; }
    public string CellContent { get; set; } = null!;
    public bool IsModifiable  { get; set; } 
}