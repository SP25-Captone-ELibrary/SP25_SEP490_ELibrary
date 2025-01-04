using FPTU_ELibrary.Application.Dtos.AIServices.Classification;

namespace FPTU_ELibrary.API.Payloads.Requests.CustomVision;

public class BaseTrainedModelRequest
{
    public List<IFormFile> ImageList { get; set; }
}

public class TrainedModelRequest :BaseTrainedModelRequest
{
    public int BookEditionId { get; set; }
}

public class TrainModelAfterCreateRequest : BaseTrainedModelRequest
{
    public Guid BookCode { get; set; }
}
public static class TrainedModelRequestExtensions
{
    public static List<TrainedBookDetailDto> ToListTrainedBookDetailDto(this List<TrainedModelRequest> req)
    {
        return req.Select(x => new TrainedBookDetailDto
        {
            BookEditionId = x.BookEditionId,
            ImageList = x.ImageList
        }).ToList();
    }
}