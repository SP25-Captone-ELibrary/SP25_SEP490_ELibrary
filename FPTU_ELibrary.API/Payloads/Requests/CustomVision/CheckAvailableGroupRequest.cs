namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class CheckAvailableGroupRequest
{
    public int RootItemId { get; set; }
    public List<int> OtherItemIds { get; set; }
}