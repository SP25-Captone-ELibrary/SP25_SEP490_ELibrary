using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Employee;

public class ImportEmployeeRequest
{
    public IFormFile? File { get; set; } = null!;
    public DuplicateHandle DuplicateHandle { get; set; }
    public string? ColumnSeparator { get; set; }
    public string? EncodingType { get; set; } 
    public string[]? ScanningFields { get; set; }
}