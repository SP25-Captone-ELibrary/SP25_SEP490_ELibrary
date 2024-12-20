namespace FPTU_ELibrary.API.Payloads.Requests.Employee;

public class UpdateRequest : UpdateProfileRequest
{
    public string? EmployeeCode { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}