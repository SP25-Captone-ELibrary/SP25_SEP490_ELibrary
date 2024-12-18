namespace FPTU_ELibrary.Domain.Specifications.Params
{
    public class EmployeeSpecParams : BaseSpecParams
    {
        public string? EmployeeCode { get; set; }
        public int? RoleId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime[]? DobRange { get; set; } 
        public DateTime[]? CreateDateRange { get; set; } 
        public DateTime[]? ModifiedDateRange { get; set; } 
        public DateTime[]? HireDateRange { get; set; } 
    }
}
