namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class ExtendTrainingRequest
{
    public IDictionary<int, List<int>> ItemIdsDic { get; }
    public IDictionary<int,List<string>> ImagesDic { get; }
    
}