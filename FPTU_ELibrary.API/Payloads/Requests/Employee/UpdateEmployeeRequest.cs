using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Employee;

public class UpdateEmployeeRequest : UpdateEmployeeProfileRequest
{
    public string? EmployeeCode { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}