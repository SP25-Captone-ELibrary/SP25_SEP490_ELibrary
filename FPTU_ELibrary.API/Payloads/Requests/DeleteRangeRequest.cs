namespace FPTU_ELibrary.API.Payloads.Requests;

public class DeleteRangeRequest<TKey>
{
    public TKey[] Ids { get; set; } = null!;
}