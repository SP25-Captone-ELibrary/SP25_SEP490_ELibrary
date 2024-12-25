using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookResourceRequest
{
    public string ResourceType { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string FileFormat { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
}