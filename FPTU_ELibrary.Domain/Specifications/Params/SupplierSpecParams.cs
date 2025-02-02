using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class SupplierSpecParams : BaseSpecParams
{
    public SupplierType? SupplierType { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime?[]? CreateDateRange { get; set; } 
    public DateTime?[]? ModifiedDateRange { get; set; }
}