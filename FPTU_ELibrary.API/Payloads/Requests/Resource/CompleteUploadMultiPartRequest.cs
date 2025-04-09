namespace FPTU_ELibrary.API.Payloads.Requests.Resource;

public class CompleteUploadMultiPartRequest
{
    public string S3PathKey { get; set; } = null!;
    public string UploadId { get; set; } = null!;
    public List<UploadedPart> UploadedParts { get; set; } = new();
}
public class UploadedPart
{
    public int PartNumber { get; set; }
    public string ETag { get; set; } = null!;
}

public static class CompleteUploadMultiPartRequestExtension
{
    public static List<(int,string)> ConvertToTuple(this CompleteUploadMultiPartRequest request)
    {
        var res = request.UploadedParts
            .Select(x => (x.PartNumber, x.ETag))
            .ToList();
        return res;
    }

}