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
    [Description("/management/library-items")] // Combine with authors, categories, resources, audit trail, library location, library cards, library card packages, library cardholders, library item conditions
    LibraryItemManagement,
    [Description("/management/borrows")] // Combine with notifications, reservations (This route has already included borrow requests and records)
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
    [Description("/management/library-cards")]
    LibraryCardManagement,
    [Description("/management/library-card-holders")]
    LibraryCardHolderManagement,
    [Description("/management/packages")]
    LibraryCardPackageManagement,
    [Description("/management/conditions")]
    LibraryItemConditionManagement,
    [Description("/management/notifications")]
    NotificationManagement,
    [Description("/management/reservations")]
    ReservationManagement,
    [Description("/management/system-messages")]
    SystemMessageManagement,
    [Description("/management/resources")]
    ResourceManagement,
    [Description("/management/library-items/audits")]
    AuditTrailManagement,
    [Description("/management/roles/audit-trails")]
    RoleAuditTrailManagement,
    [Description("/management/suppliers")]
    SupplierManagement,
}