using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum SystemFeature
{
    // System features
    [Description("/management/users")]
    UserManagement,
    [Description("/management/employees")]
    EmployeeManagement,
    [Description("/management/roles")] // Combine with permissions
    RoleManagement,
    [Description("/management/fines")]
    FineManagement,
    [Description("/management/books")] // Combine with authors, categories, resources
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
    [Description("/management/notifications")]
    NotificationManagement,
    [Description("/management/returns")]
    ReturnManagement,
    [Description("/management/system-messages")]
    SystemMessageManagement,
    [Description("/management/resources")]
    ResourceManagement
}