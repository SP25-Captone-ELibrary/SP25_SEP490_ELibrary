using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookResourceRequest
{
    public BookResourceType ResourceType { get; set; }
    public string ResourceUrl { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
}