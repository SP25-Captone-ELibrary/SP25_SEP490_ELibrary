namespace FPTU_ELibrary.API.Payloads.Requests;

public class RangeRequest<TKey>
{
    public TKey[] Ids { get; set; } = null!;
}