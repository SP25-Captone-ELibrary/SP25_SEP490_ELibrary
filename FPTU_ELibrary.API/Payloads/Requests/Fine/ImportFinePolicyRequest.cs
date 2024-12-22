using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class ImportFinePolicyRequest
{
public IFormFile File { get; set; } = null!;
    public DuplicateHandle DuplicateHandle { get; set; }    
}