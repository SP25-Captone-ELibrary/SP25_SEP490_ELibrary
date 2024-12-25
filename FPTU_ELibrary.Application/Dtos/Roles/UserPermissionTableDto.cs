using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Roles;

public class UserPermissionTableDto
{
    public List<string> ColumnHeaders { get; set; } = new(); 
    public List<UserPermissionRowDto> DataRows { get; set; } = new(); 
}

public static class UserPermissionTableDtoExtensions
{
    public static UserPermissionTableDto? ToUserPermissionTable(this List<RolePermissionDto> rolePermissions,
        bool isRoleVerticalLayout,
        int rolePermissionFeatureId,
        List<SystemRoleDto>? roles = null,
        List<SystemFeatureDto>? features = null)
    {
        // Required features list when role display vertically  
        if (isRoleVerticalLayout && (features == null || !features.Any())) return null;
        // Required roles list when feature display veritically   
        if(!isRoleVerticalLayout && (roles == null || !roles.Any())) return null;
        
        // Initialize table components
        UserPermissionTableDto? table = new(); // Table
        List<string>? colHeaders = new(); // Table header columns
        List<UserPermissionRowDto>? rows = new(); // Table rows 
        IEnumerable<IGrouping<string, RolePermissionDto>>? groups = null;
                
        // Get current system language
        var langStr = LanguageContext.CurrentLanguage;
        var langEnum = EnumExtensions.GetValueFromDescription<SystemLanguage>(langStr);
        
        switch (langEnum)
        {
            case SystemLanguage.English:
                // Table header
                colHeaders = isRoleVerticalLayout
                    ? features?.Select(ft =>
                        StringUtils.RemoveWordAndAddWhitespace(ft.EnglishName, "management")).ToList()
                    : roles?.Select(ft =>
                        StringUtils.AddWhitespaceToString(ft.EnglishName)).ToList();
                
                // Initialize table rows
                rows = new();
                groups = isRoleVerticalLayout
                    ? rolePermissions.GroupBy(rp => 
                        StringUtils.AddWhitespaceToString(rp.Role.EnglishName))
                    : rolePermissions.GroupBy(rp => 
                        StringUtils.RemoveWordAndAddWhitespace(rp.Feature.EnglishName, "management"));

                foreach (var grp in groups)
                {
                    var singleRow = new UserPermissionRowDto();
                    
                    singleRow.Cells.Add(new ()
                    {
                        CellContent = grp.Key
                    });
                    singleRow.Cells.AddRange(grp.Select(rp => new UserPermissionCellDto()
                    {
                        PermissionId = rp.PermissionId,
                        RowId = isRoleVerticalLayout ? rp.RoleId : rp.FeatureId,
                        ColId = isRoleVerticalLayout ? rp.FeatureId : rp.RoleId,
                        CellContent = StringUtils.AddWhitespaceToString(rp.Permission.EnglishName),
                        IsModifiable = rolePermissionFeatureId != rp.FeatureId 
                    }));
                    rows.Add(singleRow);
                }
                
                // Add table header
                table.ColumnHeaders = colHeaders ?? new();
                // Add table rows
                table.DataRows = rows;
                
                break;
            case SystemLanguage.Vietnamese:
                
                // Table header
                colHeaders = isRoleVerticalLayout 
                    ? features?.Select(ft => ft.VietnameseName).ToList()
                    : roles?.Select(ft => ft.VietnameseName).ToList();
                
                // Initialize table rows
                rows = new();
                groups = isRoleVerticalLayout
                    ? rolePermissions.GroupBy(rp => rp.Role.VietnameseName)
                    : rolePermissions.GroupBy(rp => rp.Feature.VietnameseName);

                foreach (var grp in groups)
                {
                    var singleRow = new UserPermissionRowDto();
                    
                    singleRow.Cells.Add(new ()
                    {
                        CellContent = grp.Key
                    });
                    singleRow.Cells.AddRange(grp.Select(rp => new UserPermissionCellDto()
                    {
                        PermissionId = rp.PermissionId,
                        RowId = isRoleVerticalLayout ? rp.RoleId : rp.FeatureId,
                        ColId = isRoleVerticalLayout ? rp.FeatureId : rp.RoleId,
                        CellContent = rp.Permission.VietnameseName,
                        IsModifiable = rolePermissionFeatureId != rp.FeatureId
                    }));
                    rows.Add(singleRow);
                }
                
                // Add table header
                table.ColumnHeaders = colHeaders ?? new();
                // Add table rows
                table.DataRows = rows;
                
                break;
            case SystemLanguage.Russian:
                // Implement later
                break;
            case SystemLanguage.Japanese:
                // Implement later
                break;
            case SystemLanguage.Korean:
                // Implement later
                break;
        }


        return table;
    }
}