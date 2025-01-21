namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

public class CreateRangeItemInstanceRequest
{
    public List<CreateItemInstanceRequest> LibraryItemInstances { get; set; } = new();
}