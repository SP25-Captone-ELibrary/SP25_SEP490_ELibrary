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
    [Description("/management/books")] // Combine with authors, categories, resources, audit trail, library location
    BookManagement,
    [Description("/management/borrows")] // Combine with notifications, returns
    BorrowManagement,
    [Description("/management/transactions")]
    TransactionManagement,
    [Description("/management/system-configurations")] // Combine with system-messages
    SystemConfigurationManagement,
    [Description("/management/system-health")]
    SystemHealthManagement,
    
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
    [Description("/management/books/audit-trails")]
    BookAuditTrailManagement,
    [Description("/management/roles/audit-trails")]
    RoleAuditTrailManagement,
}