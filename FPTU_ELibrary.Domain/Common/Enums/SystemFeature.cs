using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum SystemFeature
{
    [Description("users")]
    UserManagement,
    [Description("employees")]
    EmployeeManagement,
    [Description("roles")]
    RoleManagement,
    [Description("fines")]
    FineManagement,
    [Description("books")] // Combine with authors, categories
    BookManagement,
    [Description("borrows")] // Combine with returns
    BorrowManagement,
    [Description("transactions")]
    TransactionManagement,
    [Description("system-configurations")]
    SystemConfigurationManagement,
    [Description("system-health")]
    SystemHealthManagement
}