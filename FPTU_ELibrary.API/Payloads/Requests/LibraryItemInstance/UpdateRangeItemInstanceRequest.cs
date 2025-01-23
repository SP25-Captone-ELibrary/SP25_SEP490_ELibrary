namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

public class UpdateRangeItemInstanceRequest
{
    public List<UpdateItemInstanceWithIdRequest> LibraryItemInstances { get; set; } = new();
}