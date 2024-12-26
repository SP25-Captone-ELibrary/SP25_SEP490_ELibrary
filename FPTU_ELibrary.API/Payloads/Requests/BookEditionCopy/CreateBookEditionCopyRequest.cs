namespace FPTU_ELibrary.API.Payloads.Requests.Book;

public class CreateBookEditionCopyRequest
{
    public string Code { get; set; } = null!;
    // Good, Worn, Damaged
    public string ConditionStatus { get; set; } = null!;
}