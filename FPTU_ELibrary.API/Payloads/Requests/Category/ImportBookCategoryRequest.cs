using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Category;

public class ImportBookCategoryRequest
{
    public IFormFile File { get; set; } = null!;
    public DuplicateHandle DuplicateHandle { get; set; }
}