using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum SystemFeature
{
    // System features
    [Description("/management/users")]
    UserManagement,
    [Description("/management/employees")]
    EmployeeManagement,
    [Description("/management/roles")] // Combine with permissions, audit trail
    RoleManagement,
    [Description("/management/fines")]
    FineManagement,
    [Description("/management/library-items")] // Combine with authors, categories, resources, audit trail, library location
    LibraryItemManagement,
    [Description("/management/borrows")] // Combine with notifications, returns
    BorrowManagement,
    [Description("/management/transactions")]
    TransactionManagement,
    [Description("/management/system-configurations")] // Combine with system-messages
    SystemConfigurationManagement,
    [Description("/management/system-health")]
    SystemHealthManagement,
    [Description("/management/warehouse-tracking")] // Combine with supplier
    WarehouseTrackingManagement,
    
    // Combined Features
    [Description("/management/authors")]
    AuthorManagement,
    [Description("/management/categories")]
    CategoryManagement,
    [Description("/management/location")]
    LibraryLocationManagement,
    [Description("/management/notifications")]
    NotificationManagement,
    [Description("/management/returns")]
    ReturnManagement,
    [Description("/management/system-messages")]
    SystemMessageManagement,
    [Description("/management/resources")]
    ResourceManagement,
    [Description("/management/library-items/audits")]
    AuditTrailManagement,
    [Description("/management/roles/audit-trails")]
    RoleAuditTrailManagement,
    [Description("/management/suppliers")]
    SupplierManagement
}